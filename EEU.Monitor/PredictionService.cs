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
using NeoSmart.AsyncLock;
using Newtonsoft.Json;

namespace EEU.Monitor;

public class PredictionService : BackgroundService {
    // ReSharper disable InconsistentlySynchronizedField
    private readonly ILogger log;
    private readonly MLContext mlContext = new();
    private readonly AsyncLock systemLock = new();
    private System currentSystem = new("Unknown", "0");
    private PredictionEngine<BodyData, BodyData.ValuePrediction>? withoutGeneraPredictionEngine;
    private DataViewSchema? schema;
    private readonly IEliteDangerousApi api;
    private readonly IConfiguration configuration;
    private readonly PredictionServiceOptions options;
    private FasterKVSettings<string, System>? kvSettings;
    private readonly AsyncRateLimiter statusRateLimiter = new(TimeSpan.FromSeconds(1));
    private readonly AsyncRateLimiter checkpointRateLimiter = new(TimeSpan.FromSeconds(5));
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private Task? checkpointTask;
    private readonly AsyncRateLimiter compactionRateLimiter = new(TimeSpan.FromMinutes(1));
    private readonly AsyncLock checkpointLock = new();

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
        api.Events.On<FsdJumpEvent>(HandleFsdJump);
        this.api = api;
        this.configuration = configuration;
        options = configuration.GetSection(PredictionServiceOptions.Position).Get<PredictionServiceOptions>() ??
                  new PredictionServiceOptions();
    }

    private async Task HandleFsdJump(FsdJumpEvent @event) {
        log.LogInformation("Jumping to {SystemName} ({SystemAddress})", @event.StarSystem, @event.SystemAddress);
        await EncounterSystem(@event.SystemAddress, @event.StarSystem);
    }

    private async Task HandleLocation(LocationEvent @event, EventContext context) {
        using (await systemLock.LockAsync()) {
            if (currentSystem.SystemAddress == @event.SystemAddress) {
                return;
            }

            log.LogInformation("Switching to system {SystemName} ({SystemAddress})", @event.StarSystem, @event.SystemAddress);
            await EncounterSystem(@event.SystemAddress, @event.StarSystem);
        }
    }

    private async Task HandleStatus(StatusEvent @event) {
        if ((await statusRateLimiter.TryTakeAsync()).RateLimited()) {
            return;
        }

        await UpdatePredictions();
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

    private async Task HandleSaaSignals(SaaSignalsFoundEvent @event) {
        log.LogDebug("Received SAA signals for {BodyName}", @event.BodyName);
        await EncounterBodyDataInSystem(
            @event.SystemAddress,
            @event.BodyName,
            new BodyData {
                Count = @event.Signals.Where(s => s.Type.Local.Contains("Bio")).Select(s => s.Count).Sum(),
                Genera = string.Join(" ", @event.Genuses.Select(g => g.Name.Local).Distinct()),
            }
        );
    }

    private async Task HandleBodySignals(FssBodySignalsEvent @event) {
        log.LogDebug("Received FSS body signals for {BodyName}", @event.BodyName);
        await EncounterBodyDataInSystem(
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
        if (options.PrimeCache) {
            await PrimeCache(cancellationToken);
        }

        await api.StartAsync();
        await base.StartAsync(cancellationToken);
    }

    private async Task PrimeCache(CancellationToken cancellationToken) {
        log.LogDebug("priming cache");
        using var scope = log.BeginScope("PrimeCache");
        var journalDir = api.Config.JournalsPath;
        if (journalDir == null) {
            log.LogCritical("Journal directory not configured");
            throw new InvalidOperationException("Journal directory not configured");
        }

        // ReSharper disable once MethodSupportsCancellation
        var journalFiles = await Task.Run(() => Directory.GetFiles(journalDir, api.Config.JournalPattern));
        journalFiles = journalFiles.OrderBy(File.GetLastWriteTimeUtc)
            .SkipLast(1)
            .Where(x => File.GetLastWriteTimeUtc(x) > options.PrimeEpoch)
            .ToArray();
        foreach (var journalFile in journalFiles) {
            if (cancellationToken.IsCancellationRequested) {
                log.LogDebug("Cancellation requested, stopping prime");
                break;
            }

            log.LogDebug("Processing journal file {JournalFile}", journalFile);

            await ProcessJournalAsync(journalFile, cancellationToken);
        }

        // ReSharper disable once MethodSupportsCancellation
        using var _ = await checkpointLock.LockAsync();
        await KvStore.TakeFullCheckpointAsync(CheckpointType.Snapshot);
    }

    private async Task ProcessJournalAsync(string journalFile, CancellationToken? cancellationToken = null) {
        await using var stream = File.OpenRead(journalFile);
        var reader = new StreamReader(stream);
        var linesReader = new AsyncLinesReader(reader);
        var context = new EventContext {
            IsRaisedDuringCatchup = true,
            SourceFile = journalFile,
        };
        await foreach (var line in linesReader.WithCancellation(cancellationToken ?? cancellationTokenSource.Token)) {
            api.Events.Invoke(line, context);
        }
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

        KvSettings = new FasterKVSettings<string, System>(dbLoc) {
            TryRecoverLatest = true,
            RemoveOutdatedCheckpoints = true,
            ValueSerializer = () => new SystemSerializer(),
        };
        KvStore = new FasterKV<string, System>(kvSettings);
        checkpointTask = Checkpoint();
    }

    private async Task Checkpoint() {
        log.LogTrace("Taking checkpoint");
        while (cancellationTokenSource.IsCancellationRequested == false) {
            await checkpointRateLimiter.WaitAsync(cancellationTokenSource.Token);
            if (cancellationTokenSource.IsCancellationRequested) {
                break;
            }

            using var _ = await checkpointLock.LockAsync();
            if ((await compactionRateLimiter.TryTakeAsync()).ShouldContinue()) {
                log.LogTrace("Compacting database");
                KvStore.For(PredictionServiceFunctions.Instance).NewSession<PredictionServiceFunctions>()
                    .Compact(KvStore.Log.SafeReadOnlyAddress, CompactionType.Scan);
                log.LogTrace("Compaction complete");
                await KvStore.TakeFullCheckpointAsync(CheckpointType.FoldOver);
            } else {
                await KvStore.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, true);
            }

            log.LogTrace("Checkpoint complete");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        cancellationTokenSource.Cancel();
        await api.StopAsync();
        await (checkpointTask ?? Task.CompletedTask);
        await base.StopAsync(cancellationToken);
    }

    private async Task EncounterBody(ScanEvent body) {
        log.LogInformation("Received scan event for {BodyName}", body.BodyName);

        await EncounterSystem(body.SystemAddress, body.StarSystem);
        if (!body.IsDetailedPlanet()) {
            log.LogDebug("Skipping scan of {BodyName} as it is not a detailed scan of a planet", body.BodyName);
            return;
        }


        await EncounterBodyDataInSystem(body.SystemAddress, body.BodyName, body.ToBodyData()!);
    }

    private async Task EncounterBodyDataInSystem(string systemAddress, string bodyName, BodyData data) {
        using (await systemLock.LockAsync()) {
            await EncounterSystem(systemAddress);
            if (!await currentSystem.UpdateBody(bodyName, data)) {
                UpsertSystem(currentSystem).Wait();
            }

            await UpdatePredictions();
        }
    }

    private async Task UpdatePredictions() {
        using (await systemLock.LockAsync()) {
            var changed = false;
            foreach (var (name, d) in await currentSystem.PredictionReadyBodies()) {
                var prediction = WithoutGeneraPredictionEngine.Predict(d.Data);
                await currentSystem.UpdatePrediction(name, prediction);
                log.LogInformation("Predicted value for {BodyName} is {Value}", name, prediction.Score);
                changed = true;
            }

            foreach (var (name, d) in await currentSystem.RefinedPredictionReadyBodies()) {
                var prediction = WithGeneraPredictionEngine.Predict(d.Data);
                await currentSystem.UpdateRefinedPrediction(name, prediction);
                log.LogInformation("Refined predicted value for {BodyName} is {Value}", name, prediction.Score);
                changed = true;
            }

            if (changed) {
                await UpsertSystem(currentSystem);
            }
        }
    }

    private async Task EncounterSystem(string address, string name = "Unknown") {
        using (await systemLock.LockAsync()) {
            if (currentSystem.SystemAddress == address) {
                if (currentSystem.Name == "Unknown" && currentSystem.Name != name) {
                    currentSystem.Name = name;
                }

                return;
            }

            var sys = await RetrieveSystem(address, name);
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

        var r = await session.RMWAsync(system.SystemAddress, system);
        while (r.Status.IsPending) {
            r = await r.CompleteAsync();
        }

        var o = r.Output;
        await TakeHybridCheckpoint();

        return o;
    }

    private async Task TakeHybridCheckpoint() {
        using var _ = await checkpointLock.LockAsync();
        if (await checkpointRateLimiter.TryTakeAsync() == AsyncRateLimiter.Result.RateLimited) {
            log.LogTrace("Skipping checkpoint as rate limited");
            return;
        }

        log.LogDebug("Taking checkpoint");
        await KvStore.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, true);
    }

    private async Task HandleBodyScan(ScanEvent scanEvent) {
        await EncounterBody(scanEvent);
    }
}
