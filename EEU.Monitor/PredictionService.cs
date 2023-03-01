using System.Reflection;
using EEU.Learn.Model;
using EliteAPI.Abstractions;
using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using EliteAPI.Events.Status.Ship;
using FASTER.core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Newtonsoft.Json;

namespace EEU.Monitor;

public class PredictionService : BackgroundService {
    // ReSharper disable InconsistentlySynchronizedField
    private readonly ILogger log;
    private readonly MLContext mlContext = new();
    private readonly object systemLock = new();
    private System currentSystem = new("Unknown", "0");
    private PredictionEngine<BodyData, BodyData.ValuePrediction>? withoutGeneraPredictionEngine;
    private DataViewSchema? schema;
    private readonly IEliteDangerousApi api;
    private readonly IConfiguration configuration;
    private readonly PredictionServiceOptions options;
    private FasterKVSettings<string, System>? kvSettings;
    private object lastUpdateLock = new();
    private DateTime lastUpdate = DateTime.MinValue;

    private FasterKVSettings<string, System> KvSettings {
        get => kvSettings ?? throw new InvalidOperationException("FasterKV settings not initialized");
        set => kvSettings = value;
    }

    private FasterKV<string, System>? kvStore;

    private FasterKV<string, System> KvStore {
        get => kvStore ?? throw new InvalidOperationException("FasterKV store not initialized");
        set => kvStore = value;
    }

    private PredictionEngine<BodyData, BodyData.ValuePrediction> WithoutGeneraPredictionEngine {
        get => withoutGeneraPredictionEngine ?? throw new InvalidOperationException("Prediction engine not initialized");
        set => withoutGeneraPredictionEngine = value;
    }

    private PredictionEngine<BodyData, BodyData.ValuePrediction>? withGeneraPredictionEngine;

    private PredictionEngine<BodyData, BodyData.ValuePrediction> WithGeneraPredictionEngine {
        get => withGeneraPredictionEngine ?? throw new InvalidOperationException("Genera prediction engine not initialized");
        set => withGeneraPredictionEngine = value;
    }

    private DataViewSchema Schema {
        get => schema ?? throw new InvalidOperationException("Schema not initialized");
        set => schema = value;
    }

    public PredictionService(ILogger<PredictionService> log, IEliteDangerousApi api, IConfiguration configuration) {
        this.log = log;
        log.LogTrace("Initializing PredictionService");
        api.Events.On<ScanEvent>(HandleBodyScan);
        api.Events.On<FssBodySignalsEvent>(HandleBodySignals);
        api.Events.On<SaaSignalsFoundEvent>(HandleSaaSignals);
        api.Events.On<StatusEvent>(HandleStatus);
        api.Events.On<LocationEvent>(HandleLocation);
        this.api = api;
        this.configuration = configuration;
        options = configuration.GetSection(PredictionServiceOptions.Position).Get<PredictionServiceOptions>() ??
                  new PredictionServiceOptions();
    }

    private void HandleLocation(LocationEvent @event, EventContext context) {
        lock (systemLock) {
            if (currentSystem.SystemAddress == @event.SystemAddress) {
                return;
            }

            log.LogInformation("Switching to system {SystemName} ({SystemAddress})", @event.StarSystem, @event.SystemAddress);
            EncounterSystem(@event.SystemAddress, @event.StarSystem);
        }
    }

    private void HandleStatus(StatusEvent @event) {
        lock (lastUpdateLock) {
            if (lastUpdate.AddSeconds(1) > DateTime.UtcNow) {
                return;
            }

            lastUpdate = DateTime.UtcNow;
        }

        UpdatePredictions();
        log.LogInformation(
            "Current system: {SystemName} ({SystemAddress}) with {BodyCount} scanned bodies",
            currentSystem.Name,
            currentSystem.SystemAddress,
            currentSystem.BodyView.Count
        );

        var eligibleBodies = currentSystem.BodyView.Count(x => x.Value.Data.PredictionReady());
        if (eligibleBodies > 0) {
            log.LogInformation("Found {BodyCount} eligible bodies", eligibleBodies);
        }

        log.LogInformation("Current predictions: {Predictions}", JsonConvert.SerializeObject(currentSystem.Predictions));
        log.LogInformation("Current refined predictions: {Predictions}", JsonConvert.SerializeObject(currentSystem.RefinedPredictions));
    }

    private void HandleSaaSignals(SaaSignalsFoundEvent @event) {
        log.LogDebug("Received SAA signals for {BodyName}", @event.BodyName);
        EncounterBodyDataInSystem(
            @event.SystemAddress,
            @event.BodyName,
            new BodyData {
                Count = @event.Signals.Where(s => s.Type.Local.Contains("Bio")).Select(s => s.Count).Sum(),
                Genera = string.Join(" ", @event.Genuses.Select(g => g.Name.Local).Distinct()),
            }
        );
    }

    private void HandleBodySignals(FssBodySignalsEvent @event) {
        log.LogDebug("Received FSS body signals for {BodyName}", @event.BodyName);
        EncounterBodyDataInSystem(
            @event.SystemAddress,
            @event.BodyName,
            new BodyData { Count = @event.Signals.Where(s => s.IsBiological).Select(s => s.Count).Sum() }
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Run(() => stoppingToken.WaitHandle.WaitOne(), stoppingToken).ContinueWith(
            _ => { log.LogDebug("Stopping prediction service"); },
            stoppingToken
        );
    }

    public override async Task StartAsync(CancellationToken cancellationToken) {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream("EEU.Monitor.valuePredictionModel.zip");
        if (stream == null) {
            log.LogCritical("Could not find model resource");
            throw new InvalidOperationException("Could not find model resource");
        }

        var model = await Task.Run(() => mlContext.Model.Load(stream, out schema), cancellationToken);
        WithoutGeneraPredictionEngine = mlContext.Model.CreatePredictionEngine<BodyData, BodyData.ValuePrediction>(model, schema);

        await using var stream2 = assembly.GetManifestResourceStream("EEU.Monitor.valuePredictionModelWithGenera.zip");
        if (stream2 == null) {
            log.LogCritical("Could not find genera model resource");
            throw new InvalidOperationException("Could not find genera model resource");
        }

        var model2 = await Task.Run(() => mlContext.Model.Load(stream2, out schema), cancellationToken);
        WithGeneraPredictionEngine = mlContext.Model.CreatePredictionEngine<BodyData, BodyData.ValuePrediction>(model2, schema);

        log.LogDebug("Prediction engine initialized");
        await InitDb();
        await api.InitialiseAsync();
        await api.StartAsync();
        await base.StartAsync(cancellationToken);
    }

    private async Task InitDb() {
        var dbLoc = options.DataStorePath;
        if (dbLoc == null) {
            log.LogCritical("Data store path not configured");
            throw new InvalidOperationException("Data store path not configured");
        }

        if (!Directory.Exists(dbLoc)) {
            log.LogDebug("Creating data store directory {Path}", dbLoc);
            await Task.Run(() => Directory.CreateDirectory(dbLoc));
        }

        kvSettings = new FasterKVSettings<string, System>(dbLoc, logger: log) {
            TryRecoverLatest = true,
            RemoveOutdatedCheckpoints = true,
            ValueSerializer = () => new SystemSerializer(),
        };
        kvStore = new FasterKV<string, System>(kvSettings);
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        await api.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    private void EncounterBody(ScanEvent body) {
        log.LogInformation("Received scan event for {BodyName}", body.BodyName);

        EncounterSystem(body.SystemAddress, body.StarSystem);
        if (!body.IsDetailedPlanet()) {
            log.LogDebug("Skipping scan of {BodyName} as it is not a detailed scan", body.BodyName);
            return;
        }


        EncounterBodyDataInSystem(body.SystemAddress, body.BodyName, body.ToBodyData()!);
    }

    private void EncounterBodyDataInSystem(string systemAddress, string bodyName, BodyData data) {
        lock (systemLock) {
            EncounterSystem(systemAddress);
            if (!currentSystem.UpdateBody(bodyName, data)) {
                UpsertSystem(currentSystem).Wait();
            }

            UpdatePredictions();
        }
    }

    private void UpdatePredictions() {
        lock (systemLock) {
            var changed = false;
            foreach (var (name, d) in currentSystem.PredictionReadyBodies()) {
                var prediction = WithoutGeneraPredictionEngine.Predict(d.Data);
                currentSystem.UpdatePrediction(name, prediction);
                log.LogInformation("Predicted value for {BodyName} is {Value}", name, prediction.Score);
                changed = true;
            }

            foreach (var (name, d) in currentSystem.RefinedPredictionReadyBodies()) {
                var prediction = WithGeneraPredictionEngine.Predict(d.Data);
                currentSystem.UpdateRefinedPrediction(name, prediction);
                log.LogInformation("Refined predicted value for {BodyName} is {Value}", name, prediction.Score);
                changed = true;
            }

            if (changed) {
                UpsertSystem(currentSystem).Wait();
            }
        }
    }

    private void EncounterSystem(string address, string name = "Unknown") {
        lock (systemLock) {
            if (currentSystem.SystemAddress == address) {
                if (currentSystem.Name == "Unknown" && currentSystem.Name != name) {
                    currentSystem.Name = name;
                }

                return;
            }

            var sys = RetrieveSystem(address).Result;
            log.LogInformation("Encountered system {SystemName} ({SystemAddress})", sys.Name, sys.SystemAddress);
            currentSystem = sys;
        }
    }

    private async Task<System> RetrieveSystem(string address, string name = "Unknown") {
        using var session = KvStore.For(PredictionServiceFunctions.Instance)
            .NewSession<PredictionServiceFunctions>();

        var sys = new System(name, address);
        var result = await session.ReadAsync(address, sys);
        var res = result.Complete();
        return res.output ?? sys;
    }

    private async Task<System> UpsertSystem(System system) {
        using var session = KvStore.For(PredictionServiceFunctions.Instance)
            .NewSession<PredictionServiceFunctions>();

        var r = await session.UpsertAsync(system.SystemAddress, system);
        while (r.Status.IsPending) {
            r = await r.CompleteAsync();
        }

        var o = r.Output;
        await KvStore.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, true);

        return o;
    }

    private void HandleBodyScan(ScanEvent scanEvent, EventContext context) {
        EncounterBody(scanEvent);
    }
}

public class PredictionServiceOptions {
    public const string Position = "PredictionService";

    public string DataStorePath { get; set; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        nameof(EEU),
        nameof(Monitor),
        "dataStore"
    );

    public bool PrimeCache { get; set; } = true;
    public DateTime PrimeCacheFrom { get; set; } = DateTime.UtcNow.AddDays(-7);
}

public class PredictionServiceFunctions : SimpleFunctions<string, System> {
    private static System Merge(System a, System b) {
        return a.Merge(b);
    }

    private PredictionServiceFunctions() : base(Merge) { }

    public static PredictionServiceFunctions Instance { get; } = new();
}
