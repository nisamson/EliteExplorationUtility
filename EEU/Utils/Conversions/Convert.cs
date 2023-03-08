// EliteExplorationUtility - EEU - Convert.cs
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

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace EEU.Utils.Conversions;

public static class Convert {
    public static ulong ParseULong(this string s) {
        var res = 0UL;
        // first direct parse
        if (ulong.TryParse(s, NumberStyles.Any, null, out res)) {
            return res;
        }

        decimal d;
        if (decimal.TryParse(s, NumberStyles.Any, null, out d)) {
            res = (ulong) d;
            return res;
        }

        throw new ArgumentException($"couldn't parse {s}");
    }

    public class ULongConverter : DefaultTypeConverter {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) {
            Assert.NotNull(text);
            return text.ParseULong();
        }
    }
}
