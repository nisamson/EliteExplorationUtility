using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace EEU.Monitor.Util;

public class TaskService<TState> : IDisposable where TState : class {
    private readonly AsyncWaitGate startupGate;
    private readonly AsyncWaitGate shutdownGate = new();
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Thread thread;
    private readonly BufferBlock<Action<TState, ulong>> buffer = new();
    private readonly ILogger? log;
    private readonly StateInitializer initializer;
    private ulong eventsSeen;
    private TState? state;

    private TState State {
        get {
            if (state == null) {
                throw new InvalidOperationException("State not initialized");
            }

            return state;
        }
    }

    public delegate TState StateInitializer();


    public TaskService(StateInitializer initializer,
        ILogger? log = null,
        CancellationToken cancellationToken = default) {
        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.log = log;
        startupGate = new AsyncWaitGate(cancellationToken);
        this.initializer = initializer;
        thread = new Thread(EventLoop);
        thread.Start();
    }

    public Task WaitForStartupAsync(CancellationToken cancellationToken = default) {
        return startupGate.WaitAsync(cancellationToken);
    }

    private void EventLoop() {
        log?.LogDebug("Starting event loop");
        try {
            state = initializer();
            startupGate.Release();
            foreach (var action in buffer
                         .ReceiveAllAsync(cancellationTokenSource.Token)
                         .ToBlockingEnumerable()) {
                log?.LogTrace("Waiting for event");
                var eventId = Interlocked.Increment(ref eventsSeen);
                log?.LogTrace("Handling event {EventId}", eventId);
                action(state, eventId);
            }
        } catch (OperationCanceledException) {
            log?.LogDebug("Event loop cancelled");
        } catch (Exception e) {
            log?.LogError(e, "Event loop failed");
        } finally {
            log?.LogDebug("Event loop stopped");
            shutdownGate.Release();
        }
    }

    private async Task DoAsync(Action<TState, ulong> action, CancellationToken cancellationToken = default) {
        var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);
        var oneshot = new AsyncWaitGate(combined.Token);

        void Handler(TState state, ulong eventId) {
            try {
                action(state, eventId);
                oneshot.Release();
            } catch (Exception e) {
                log?.LogError(e, "Event handler failed");
            }
        }

        await buffer.SendAsync(Handler, combined.Token);
        await oneshot.WaitAsync(combined.Token);
    }

    public async Task DoAsync(Action<TState> action, CancellationToken cancellationToken = default) {
        await DoAsync((state, _) => action(state), cancellationToken);
    }

    public async Task<TResult> DoAsync<TResult>(Func<TState, ulong, TResult> action, CancellationToken cancellationToken = default) {
        TResult result = default!;
        await DoAsync((state, eventId) => result = action(state, eventId), cancellationToken);
        return result;
    }

    public async Task<TResult> DoAsync<TResult>(Func<TState, TResult> action, CancellationToken cancellationToken = default) {
        return await DoAsync((state, _) => action(state), cancellationToken);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default) {
        buffer.Complete();
        cancellationTokenSource.Dispose();
        await shutdownGate.WaitAsync(cancellationToken);
    }

    public void Shutdown() {
        buffer.Complete();
        cancellationTokenSource.Dispose();
        thread.Join();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        Shutdown();
        if (state is IDisposable disposable) {
            disposable.Dispose();
        }
    }
}
