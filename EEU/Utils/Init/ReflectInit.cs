using System.Reflection;

namespace EEU.Utils.Init;

public static class ReflectInit<TDest> where TDest : class {
    public static void From<TSrc>(TDest dest, TSrc src)
        where TSrc : class, TDest {
        var fis = dest.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var fi in fis) {
            fi.SetValue(dest, fi.GetValue(src));
        }

        var pis = dest.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var pi in pis) {
            pi.SetValue(dest, pi.GetValue(src));
        }
    }
}
