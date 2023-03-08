// EliteExplorationUtility - EEU.Monitor - PredictionService.cs
// Copyright (C) 2023 Nick Samson
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;
using System.Reflection;
using EEU.Learn.Model;
using EEU.Monitor.Elite;
using EEU.Monitor.SystemStore;
using EEU.Monitor.Util;
using EliteAPI.Abstractions;
using EliteAPI.Abstractions.Events;
using EliteAPI.Events;
using EliteAPI.Events.Status.Ship;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using NeoSmart.AsyncLock;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EEU.Monitor.Prediction;

public class PredictionService : IHostedService, IDisposable {
    // ReSharper disable InconsistentlySynchronizedField
    private readonly ILogger<PredictionService> log;
    private readonly MLContext mlContext = new();
    private readonly AsyncLock systemLock = new();
    private Elite.System currentSystem = new(null, "0");
    private PredictionEngine<BodyData, BodyData.ValuePrediction>? withoutGeneraPredictionEngine;
    private DataViewSchema? schema;
    private readonly IEliteDangerousApi api;
    private readonly PredictionServiceOptions options;
    private readonly AsyncRateLimiter statusRateLimiter = new(TimeSpan.FromSeconds(1));
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ISystemStore systemStore;

    private static ImmutableList<Type> RelevantEventTypes { get; } = ImmutableList.Create(
        typeof(ScanEvent),
        typeof(FssBodySignalsEvent),
        typeof(StatusEvent),
        typeof(SaaSignalsFoundEvent),
        typeof(LocationEvent),
        typeof(FsdJumpEvent)
    );

    private PredictionEngine<BodyData, BodyData.ValuePrediction> WithoutGeneraPredictionEngine {
        get => withoutGeneraPredictionEngine ?? throw new InvalidOperationException("Prediction engine not initialized");
        set => withoutGeneraPredictionEngine = value;
    }

    private PredictionEngine<BodyData, BodyData.ValuePrediction>? withGeneraPredictionEngine;

    private PredictionEngine<BodyData, BodyData.ValuePrediction> WithGeneraPredictionEngine {
        get => withGeneraPredictionEngine ?? throw new InvalidOperationException("Genera prediction engine not initialized");
        set => withGeneraPredictionEngine = value;
    }

    private object SystemLock => systemLock;

    public PredictionService(ILogger<PredictionService> log, IEliteDangerousApi api, ISystemStore store, IConfiguration configuration) {
        this.log = log;
        log.LogTrace("Initializing PredictionService");
        systemStore = store;
        api.Events.On<ScanEvent>(HandleBodyScan);
        api.Events.On<FssBodySignalsEvent>(HandleBodySignals);
        api.Events.On<SaaSignalsFoundEvent>(HandleSaaSignals);
        api.Events.On<StatusEvent>(HandleStatus);
        api.Events.On<LocationEvent>(HandleLocation);
        api.Events.On<FsdJumpEvent>(HandleFsdJump);
        this.api = api;
        options = configuration.GetSection(PredictionServiceOptions.Position).Get<PredictionServiceOptions>() ??
                  new PredictionServiceOptions();
    }

    private void HandleFsdJump(FsdJumpEvent @event) {
        log.LogInformation("Jumping to {SystemName} ({SystemAddress})", @event.StarSystem, @event.SystemAddress);
        EncounterSystem(@event.SystemAddress, @event.StarSystem);
    }

    private void HandleLocation(LocationEvent @event) {
        log.LogTrace("Received location event for {SystemName} ({SystemAddress})", @event.StarSystem, @event.SystemAddress);
        lock (SystemLock) {
            if (currentSystem.SystemAddress == @event.SystemAddress) {
                return;
            }

            log.LogDebug("Switching to system {SystemName} ({SystemAddress})", @event.StarSystem, @event.SystemAddress);
            EncounterSystem(@event.SystemAddress, @event.StarSystem);
        }
    }

    private void HandleStatus(StatusEvent @event) {
        log.LogTrace("Received status event");
        if (statusRateLimiter.TryTakeAsync(cancellationTokenSource.Token).Result.RateLimited()) {
            return;
        }

        UpdatePredictions();
        log.LogInformation(
            "Current system: {SystemName} ({SystemAddress}) with {BodyCount} scanned candidate bodies",
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

    public async Task StartAsync(CancellationToken cancellationToken) {
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
        await systemStore.StartAsync(cancellationToken);
        await api.InitialiseAsync();
        if (options.PrimeCache) {
            await PrimeCache(cancellationToken);
        }

        await api.StartAsync();
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
    }

    private async Task ProcessJournalAsync(string journalFile, CancellationToken cancellationToken = default) {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
        await using var stream = File.OpenRead(journalFile);
        var reader = new StreamReader(stream);
        var linesReader = new AsyncLinesReader(reader);
        await foreach (var line in linesReader.WithCancellation(linkedCts.Token)) {
            using var _ = log.BeginScope("ProcessJournalAsync {JournalFile}", journalFile);

            bool PrefixedName(Type eType, string e) {
                return eType.Name.StartsWith(e, StringComparison.OrdinalIgnoreCase);
            }

            // api.Events.Invoke(
            //     line,
            //     new EventContext {
            //         IsRaisedDuringCatchup = true,
            //         SourceFile = journalFile,
            //     }
            // );

            IEvent[] relevantEvents = RelevantEventTypes
                .Where(
                    eType => {
                        var jTok = JObject.Parse(line)["event"];
                        var s = jTok?.Value<string>();
                        return s != null && PrefixedName(eType, s);
                    }
                )
                .Select(eType => api.EventParser.FromJson(eType, line))
                .Where(x => x != null)
                .ToArray()!;

            if (relevantEvents.Length > 1) {
                log.LogCritical("Multiple possible events for line {Line}", line);
                log.LogCritical("Events: {Events}", JsonConvert.SerializeObject(relevantEvents));
                throw new InvalidOperationException("Multiple possible events for line");
            }

            if (relevantEvents.Length == 0) {
                // log.LogTrace("No relevant events for line. Skipping.");
                continue;
            }

            log.LogTrace("Processing line {Line}", line);
            await Task.Run(
                () => {
                    switch (relevantEvents.First()) {
                        case ScanEvent scanEvent:
                            HandleBodyScan(scanEvent);
                            break;
                        case FssBodySignalsEvent fssBodySignalsEvent:
                            HandleBodySignals(fssBodySignalsEvent);
                            break;
                        case FsdJumpEvent fsdJumpEvent:
                            HandleFsdJump(fsdJumpEvent);
                            break;
                        case StatusEvent statusEvent:
                            HandleStatus(statusEvent);
                            break;
                        case SaaSignalsFoundEvent saaSignalsFoundEvent:
                            HandleSaaSignals(saaSignalsFoundEvent);
                            break;
                        case LocationEvent locationEvent:
                            HandleLocation(locationEvent);
                            break;
                    }
                },
                linkedCts.Token
            );
        }
    }


    public Task StopAsync(CancellationToken cancellationToken) {
        cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    private void EncounterBody(ScanEvent body) {
        log.LogDebug("Received scan event for {BodyName}", body.BodyName);

        EncounterSystem(body.SystemAddress, body.StarSystem);
        if (!body.IsDetailedPlanet()) {
            log.LogTrace("Skipping scan of {BodyName} as it is not a detailed scan of a planet", body.BodyName);
            return;
        }


        EncounterBodyDataInSystem(body.SystemAddress, body.BodyName, body.ToBodyData()!);
    }

    private void EncounterBodyDataInSystem(string systemAddress, string bodyName, BodyData data) {
        log.LogTrace("Encountering body data for {BodyName} in system {SystemAddress}", bodyName, systemAddress);
        lock (SystemLock) {
            EncounterSystem(systemAddress);
            if (!currentSystem.UpdateBody(bodyName, data)) {
                currentSystem = UpsertSystem(currentSystem);
            }

            UpdatePredictions();
        }
    }

    private void UpdatePredictions() {
        log.LogTrace("Updating predictions");
        lock (SystemLock) {
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
                UpsertSystem(currentSystem);
            }
        }
    }

    private void EncounterSystem(string address, string? name = null) {
        log.LogTrace("Encountering system {SystemAddress}", address);
        lock (SystemLock) {
            if (currentSystem.SystemAddress == address) {
                if (currentSystem.NameIsUnknown && name != null) {
                    currentSystem.Name = name;
                }

                return;
            }

            var sys = RetrieveSystem(address, name);
            log.LogDebug("Encountered system {SystemName} ({SystemAddress})", sys.Name, sys.SystemAddress);
            currentSystem = sys;
        }
    }

    private Elite.System RetrieveSystem(string address, string? name = null) {
        return systemStore.GetSystemAsync(address, name, cancellationTokenSource.Token).Result;
    }

    private Elite.System UpsertSystem(Elite.System system) {
        return systemStore.MergeSystemAsync(system, cancellationTokenSource.Token).Result;
    }

    private void HandleBodyScan(ScanEvent scanEvent) {
        log.LogTrace("Handling body scan");
        EncounterBody(scanEvent);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        withoutGeneraPredictionEngine?.Dispose();
        cancellationTokenSource.Dispose();
        withGeneraPredictionEngine?.Dispose();
    }
}
