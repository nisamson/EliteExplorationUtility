// EliteExplorationUtility - EEU - ReflectInit.cs
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
