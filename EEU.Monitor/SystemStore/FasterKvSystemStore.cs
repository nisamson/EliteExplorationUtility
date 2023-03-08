// EliteExplorationUtility - EEU.Monitor - FasterKvSystemStore.cs
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

using EEU.Monitor.KvSupport;
using EEU.Monitor.Prediction;
using EEU.Monitor.Util;
using FASTER.core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;

namespace EEU.Monitor.SystemStore;

public class FasterKvSystemStore : BackgroundService, ISystemStore {
    private readonly AsyncLock checkpointLock = new();
    private readonly AsyncRateLimiter checkpointRateLimiter;
    private readonly AsyncRateLimiter compactionRateLimiter;

    private readonly Configuration config;
    private readonly FasterKVSettings<string, Elite.System> kvSettings;

    private readonly ILogger<FasterKvSystemStore> log;
    private FasterKV<string, Elite.System>? kvStore;

    public FasterKvSystemStore(IConfiguration config, ILogger<FasterKvSystemStore> log) {
        this.log = log;
        this.config = config.GetSection(ISystemStore.Configuration.Position).Get<Configuration>() ?? new Configuration();
        compactionRateLimiter = new AsyncRateLimiter(this.config.CompactionRate);
        checkpointRateLimiter = new AsyncRateLimiter(this.config.CheckpointRate);
        var dbLoc = this.config.DbPath;
        if (dbLoc == null) {
            log.LogCritical("Data store path not configured");
            throw new InvalidOperationException("Data store path not configured");
        }

        if (!Directory.Exists(dbLoc)) {
            log.LogDebug("Creating data store directory {Path}", dbLoc);
            Directory.CreateDirectory(dbLoc);
        }

        kvSettings = new FasterKVSettings<string, Elite.System>(dbLoc) {
            TryRecoverLatest = true,
            RemoveOutdatedCheckpoints = true,
            ValueSerializer = () => new SystemSerializer(),
        };
    }

    private FasterKV<string, Elite.System> KvStore {
        get => kvStore ?? throw new InvalidOperationException("FasterKV store not initialized");
        set => kvStore = value;
    }

    public ISystemStore.Configuration Config => config;

    public async Task<Elite.System> GetSystemAsync(string address,
        string? systemName = null,
        CancellationToken cancellationToken = default) {
        using var session = KvStore.For(PredictionServiceFunctions.Instance)
            .NewSession<PredictionServiceFunctions>();

        var sys = new Elite.System(systemName, address);
        var result = await session.ReadAsync(address, sys, token: CancellationToken.None);
        var res = await Task.Run(() => result.Complete(), CancellationToken.None);
        return res.output ?? sys;
    }

    public async Task<Elite.System> MergeSystemAsync(Elite.System system, CancellationToken cancellationToken = default) {
        using var session = KvStore.For(PredictionServiceFunctions.Instance)
            .NewSession<PredictionServiceFunctions>();

        var r = await session.RMWAsync(system.SystemAddress, system, token: CancellationToken.None);
        while (r.Status.IsPending) {
            r = await r.CompleteAsync(CancellationToken.None);
        }

        var o = r.Output;

        return o;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        KvStore = new FasterKV<string, Elite.System>(kvSettings);
        await Checkpoint(stoppingToken);
    }

    private async Task Checkpoint(CancellationToken cancellationToken) {
        log.LogTrace("taking checkpoint");
        while (!cancellationToken.IsCancellationRequested) {
            await checkpointRateLimiter.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            using var _ = await checkpointLock.LockAsync(CancellationToken.None);
            if ((await compactionRateLimiter.TryTakeAsync()).ShouldContinue()) {
                log.LogTrace("compacting database");
                KvStore.For(PredictionServiceFunctions.Instance).NewSession<PredictionServiceFunctions>()
                    .Compact(KvStore.Log.SafeReadOnlyAddress);
                log.LogTrace("compaction complete");
                await KvStore.TakeFullCheckpointAsync(CheckpointType.FoldOver, CancellationToken.None);
            } else {
                await KvStore.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, true, CancellationToken.None);
            }

            log.LogTrace("checkpoint complete");
        }
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

    public override void Dispose() {
        GC.SuppressFinalize(this);
        KvStore.Dispose();
        base.Dispose();
    }

    public class Configuration : ISystemStore.Configuration {
        public TimeSpan CheckpointRate { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan CompactionRate { get; set; } = TimeSpan.FromMinutes(1);
    }
}
