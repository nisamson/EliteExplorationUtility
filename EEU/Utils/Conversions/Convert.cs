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
