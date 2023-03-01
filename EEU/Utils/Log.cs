using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EEU.Utils;

public class Log {
    public ILogger BackingLogger { get; set; } = NullLogger.Instance;

    private static readonly Once<Log> Instance = new(() => new Log());

    public static ILogger Ger => Instance.Elem.BackingLogger;


    private Log() { }

    public static Log Logger => Instance.Elem;
}

public static class Formatters {
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
}
