using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using EFCore.BulkExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EEU.Model;

public static class Loader {
    private static readonly int MaxJsonBufferCount = 1024 * 10;
    
    public static void BulkUpsertFromFile(this EEUContext ctx, FileStream file) {
        using GZipStream gunzipped = new(file, CompressionMode.Decompress);
        using JsonTextReader jr = new(new StreamReader(gunzipped, Encoding.UTF8));

        

        jr.SupportMultipleContent = true;
        var serializer = new JsonSerializer {
            // '2023-01-19 20:56:31+00'
            // ReSharper disable once StringLiteralTypo
            DateFormatString = "yyyy-MM-dd HH:mm:sszz",
            DefaultValueHandling = DefaultValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
        };

        using var tx = ctx.Database.BeginTransaction();
        var bc = new BulkConfig {
            BatchSize = MaxJsonBufferCount,
            IncludeGraph = true,
        };
        var chunkCnt = 0;
        foreach (var chunk in ReadSystems(serializer, jr).Chunk(MaxJsonBufferCount)) {
            chunkCnt++;
            var cnt = chunkCnt;
            ctx.BulkInsertOrUpdate(chunk, bc, d => Debug.WriteLine($"chunk {cnt} bulk loading {d}% done"));
        }
        tx.Commit();
    }

    private static IEnumerable<System> ReadSystems(JsonSerializer serializer, JsonReader jr) {
        while (jr.Read() && jr.TokenType != JsonToken.EndArray) {
            var o = serializer.Deserialize<System>(jr);
            yield return o!;
        }
    }
}
