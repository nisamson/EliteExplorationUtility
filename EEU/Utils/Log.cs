// EliteExplorationUtility - EEU - Log.cs
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EEU.Utils;

public class Log {
    private static readonly Once<Log> Instance = new(() => new Log());


    private Log() { }
    public ILogger BackingLogger { get; set; } = NullLogger.Instance;

    public static ILogger Ger => Instance.Elem.BackingLogger;

    public static Log Logger => Instance.Elem;
}

public static class Formatters {
    private static readonly ImmutableArray<TimeSpanAccessor> TimeNames = ImmutableArray.Create(
        new TimeSpanAccessor("d", span => span.Days),
        new TimeSpanAccessor("h", span => span.Hours),
        new TimeSpanAccessor("m", span => span.Minutes),
        new TimeSpanAccessor("s", span => span.Seconds),
        new TimeSpanAccessor("ms", span => span.Milliseconds),
        new TimeSpanAccessor("μs", span => span.Microseconds),
        new TimeSpanAccessor("ns", span => span.Nanoseconds)
    );

    private static IEnumerable<string> TimeSpanComponents(TimeSpan span) {
        return TimeNames
            .Select(accessor => new TimeSpanComponent(accessor, span))
            .Where(c => c.Count != 0)
            .Select(c => c.ToString());
    }

    public static string Humanize(this TimeSpan span, int? maxComponents = null) {
        var max = maxComponents ?? TimeNames.Length;
        var o = string.Join(" ", TimeSpanComponents(span).Take(max));
        return o.Length == 0 ? "0s" : o;
    }

    private class TimeSpanAccessor {
        public TimeSpanAccessor(string shortName, Func<TimeSpan, int> accessor) {
            ShortName = shortName;
            Accessor = accessor;
        }

        public string ShortName { get; }
        public Func<TimeSpan, int> Accessor { get; }
    }

    private struct TimeSpanComponent {
        public TimeSpanComponent(string shortName, int count) {
            ShortName = shortName;
            Count = count;
        }

        public TimeSpanComponent(TimeSpanAccessor accessor, TimeSpan span) : this(accessor.ShortName, accessor.Accessor(span)) { }

        public string ShortName { get; }
        public int Count { get; }

        public override string ToString() {
            return $"{Count}{ShortName}";
        }
    }
}
