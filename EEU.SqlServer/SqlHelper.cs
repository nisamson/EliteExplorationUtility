using System;
using System.Linq;

namespace EEU.SqlServer;

using Microsoft.SqlServer.Server;

public class SqlHelper {
    [SqlFunction(IsDeterministic = true, IsPrecise = true)]
    public static string? SplitOrdinal(string s, string separator, int ordinal) {
        var split = s.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        return split.ElementAtOrDefault(ordinal - 1);
    }
}
