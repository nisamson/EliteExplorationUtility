using System.Diagnostics.CodeAnalysis;

namespace EEU.Utils;

internal static class Assert {
    public static T NotNull<T>([NotNull] T? value, string? valueExpression = null) {
        return value ?? throw new ArgumentNullException(nameof(value), valueExpression);
    }
}
