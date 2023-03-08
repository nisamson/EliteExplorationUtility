// EliteExplorationUtility - EEU.SqlServer - SqlHelper.cs
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

using System;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace EEU.SqlServer;

public class SqlHelper {
    [SqlFunction(IsDeterministic = true, IsPrecise = true)]
    public static string? SplitOrdinal(string s, string separator, int ordinal) {
        var split = s.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        return split.ElementAtOrDefault(ordinal - 1);
    }
}
