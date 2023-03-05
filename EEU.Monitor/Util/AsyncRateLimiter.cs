using NeoSmart.AsyncLock;

namespace EEU.Monitor.Util;

public class AsyncRateLimiter {
    public enum Result {
        RateLimited = 0,
        Success,
    }

    private readonly AsyncLock updateLock = new();
    private readonly TimeSpan updatePeriod;
    private DateTime lastUpdate = DateTime.MinValue;

    public AsyncRateLimiter(TimeSpan updatePeriod) {
        this.updatePeriod = updatePeriod;
    }

    /// <summary>
    /// Tries to take a value from the rate limiter.
    /// </summary>
    /// <returns>Success if the value was taken, or RateLimited if the rate limiter is not ready to take another value.</returns>
    public async Task<Result> TryTakeAsync(CancellationToken cancellationToken = default) {
        using (await updateLock.LockAsync(cancellationToken)) {
            if (DateTime.Now - lastUpdate < updatePeriod) {
                return Result.RateLimited;
            }

            lastUpdate = DateTime.Now;
            return Result.Success;
        }
    }

    /// <summary>
    /// Wait until the rate limiter is ready to take another value. Uses exponential backoff.
    /// </summary>
    public async Task WaitAsync(CancellationToken cancellationToken = default) {
        var curMultiplier = 1;
        try {
            while (!cancellationToken.IsCancellationRequested && await TryTakeAsync(cancellationToken) == Result.RateLimited) {
                await Task.Delay(updatePeriod * curMultiplier, cancellationToken);
                curMultiplier *= 2;
            }
        } catch (TaskCanceledException) {
            // Ignore because we were waiting anyways
        }
    }
}

public static class ResultHelper {
    public static bool ShouldContinue(this AsyncRateLimiter.Result result) {
        return result == AsyncRateLimiter.Result.Success;
    }

    public static bool RateLimited(this AsyncRateLimiter.Result result) {
        return result == AsyncRateLimiter.Result.RateLimited;
    }
}
