namespace EEU.Monitor.Util;

public class AsyncWaitGate {
    private readonly TaskCompletionSource tcs = new();
    private readonly CancellationToken cancellationToken;

    public AsyncWaitGate(CancellationToken cancellationToken = default) {
        this.cancellationToken = cancellationToken;
    }

    public void Release() {
        tcs.SetResult();
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default) {
        var combined = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationToken, cancellationToken);
        await tcs.Task.WaitAsync(combined.Token);
    }
}
