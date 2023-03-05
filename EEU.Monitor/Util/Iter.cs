namespace EEU.Monitor.Util;

public static class IterExt {
    public static T? FirstNotNull<T>(this IEnumerable<T?> source) where T : class {
        return source.FirstOrDefault(item => item != null);
    }

    public static T? AcceptIf<T>(this T? source, Func<T, bool> predicate) where T : class {
        if (source == null) {
            return null;
        }

        return predicate(source) ? source : null;
    }
}
