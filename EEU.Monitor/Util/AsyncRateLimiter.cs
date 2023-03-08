// EliteExplorationUtility - EEU.Monitor - AsyncRateLimiter.cs
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
    ///     Tries to take a value from the rate limiter.
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
    ///     Wait until the rate limiter is ready to take another value. Uses exponential backoff.
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
