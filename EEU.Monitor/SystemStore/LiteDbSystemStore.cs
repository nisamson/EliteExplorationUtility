using System.Threading.Tasks.Dataflow;
using EEU.Monitor.Util;
using LiteDB;
using LiteDB.Async;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System = EEU.Monitor.Elite.System;

namespace EEU.Monitor.SystemStore;

public class LiteDbSystemStore : BackgroundService, ISystemStore {
    private readonly ILogger<LiteDbSystemStore> log;
    private readonly ISystemStore.Configuration config;
    private Executor? executor;

    private Executor DbExecutor {
        get {
            if (executor == null) {
                throw new InvalidOperationException("Executor not initialized");
            }

            return executor;
        }
    }

    private delegate void DbEvent(LiteDatabase db, ILiteCollection<Elite.System> systems, ulong eventId);


    private AsyncWaitGate startupGate;

    private class Executor : IDisposable {
        private readonly LiteDatabase database;
        private readonly ILiteCollection<Elite.System> systems;
        private readonly ILogger<LiteDbSystemStore> log;
        private readonly Thread thread;
        private readonly BufferBlock<DbEvent> buffer = new();
        private readonly CancellationToken cancellationToken;
        private ulong eventsSeen;

        public Executor(ISystemStore.Configuration config, ILogger<LiteDbSystemStore> log, CancellationToken cancellationToken) {
            this.log = log;
            Directory.CreateDirectory(config.DbPath);
            var dbPath = Path.Join(config.DbPath, "systems.db");
            database = new LiteDatabase(
                new ConnectionString {
                    Filename = dbPath,
                    Upgrade = true,
                    Connection = ConnectionType.Direct,
                }
            );
            this.cancellationToken = cancellationToken;
            systems = database.GetCollection<Elite.System>("systems");
            thread = new Thread(HandleEvents);
            thread.Start();
        }

        private void HandleEvents() {
            log.LogTrace("Starting LiteDbSystemStore event handler");
            foreach (var dbEvent in buffer.ReceiveAllAsync(cancellationToken).ToEnumerable()) {
                var eventId = Interlocked.Increment(ref eventsSeen);
                log.LogTrace("executing event {EventId}", eventId);
                dbEvent(database, systems, eventId);
            }
        }

        public async Task DoAsync(DbEvent dbEvent, CancellationToken cancellationToken = default) {
            await DoAsync<object>(
                (db, systems, eventId) => {
                    dbEvent(db, systems, eventId);
                    return null;
                },
                cancellationToken
            );
        }

        public async Task<TResult> DoAsync<TResult>(Func<LiteDatabase, ILiteCollection<Elite.System>, ulong, TResult> dbEvent,
            CancellationToken cancellationToken = default) {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationToken, cancellationToken);
            var oneshot = new TaskCompletionSource<TResult>();

            void Wrapped(LiteDatabase db, ILiteCollection<Elite.System> systems, ulong eventId) {
                oneshot.SetResult(dbEvent(db, systems, eventId));
            }

            await buffer.SendAsync(Wrapped, linked.Token);
            return await oneshot.Task;
        }


        public void Dispose() {
            GC.SuppressFinalize(this);
            buffer.Complete();
            thread.Join();
            database.Dispose();
        }
    }

    public LiteDbSystemStore(ILogger<LiteDbSystemStore> log, IConfiguration config) {
        this.log = log;
        this.config = config.GetSection(ISystemStore.Configuration.Position).Get<ISystemStore.Configuration>() ??
                      new ISystemStore.Configuration();
    }

    public override async Task StartAsync(CancellationToken cancellationToken) {
        startupGate = new AsyncWaitGate(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        executor = new Executor(config, log, stoppingToken);
        startupGate.Release();
        var tc = new TaskCompletionSource();
        stoppingToken.Register(() => tc.SetResult());
        await tc.Task;
    }

    public override void Dispose() {
        GC.SuppressFinalize(this);
        executor?.Dispose();
        base.Dispose();
    }

    public ISystemStore.Configuration Config => config;

    public async Task<Elite.System> GetSystemAsync(string address,
        string? systemName = null,
        CancellationToken cancellationToken = default) {
        var result = await DbExecutor.DoAsync(
            (db, systems, eventId) => {
                log.LogTrace("Looking up system at address ({address}), transaction {eventId}", address, eventId);
                return systems.FindById(address);
            },
            cancellationToken
        );
        if (result?.NameIsUnknown == true && systemName != null) {
            result.Name = systemName;
            await DbExecutor.DoAsync(
                (db, systems, eventId) => {
                    log.LogTrace("Updating system {system}, transaction {eventId}", result, eventId);
                    systems.Update(result);
                },
                cancellationToken
            );
        }

        return result ?? new Elite.System(systemName, address);
    }

    public Task<Elite.System> MergeSystemAsync(Elite.System system, CancellationToken cancellationToken = default) {
        return DbExecutor.DoAsync(
            (db, systems, eventId) => {
                log.LogTrace("Merging system {system}, transaction {eventId}", system, eventId);
                db.BeginTrans();
                try {
                    var existing = systems.FindById(system.SystemAddress);
                    if (existing != null) {
                        log.LogTrace("System {system} already exists, merging, transaction {eventId}", system, eventId);
                        existing = existing.Merge(system);
                        systems.Update(existing);
                        db.Commit();
                        log.LogTrace("System {system} merged, transaction {eventId}", system, eventId);
                        return existing;
                    }

                    systems.Insert(system);
                    db.Commit();
                    log.LogTrace("System {system} merged, transaction {eventId}", system, eventId);
                    return system;
                } catch (Exception e) {
                    log.LogError(e, "Failed to merge system {system}, transaction {eventId}", system, eventId);
                    db.Rollback();
                    throw;
                }
            },
            cancellationToken
        );
    }
}
