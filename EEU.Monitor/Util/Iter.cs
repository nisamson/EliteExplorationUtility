// EliteExplorationUtility - EEU.Monitor - Iter.cs
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
