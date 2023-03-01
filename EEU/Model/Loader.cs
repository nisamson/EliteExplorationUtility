using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks.Dataflow;
using EEU.Utils;
using EFCore.BulkExtensions;
using Humanizer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EEU.Model;

public static class Loader {
    private const int MaxJsonBufferCount = 1024;

    public static void BulkUpsertFromFile(DbContextOptionsBuilder<EEUContext> b, FileStream file, long skip = 0) {
        // using GZipStream gunzipped = new(file, CompressionMode.Decompress);
        b.UseSqlServer(s => s.EnableRetryOnFailure(15));
        file.Seek(skip, SeekOrigin.Begin);
        using var sr = new StreamReader(file, Encoding.UTF8, bufferSize: 1024 * 16);
        sr.ReadLine();
        var lines = sr.Lines()
            .Select(s => s.AsSpan().Trim().TrimEnd(",").ToString())
            .Where(x => x.StartsWith("{"));

        var serializer = new JsonSerializer {
            // '2023-01-19 20:56:31+00'
            // ReSharper disable once StringLiteralTypo
            DateFormatString = "yyyy-MM-dd HH:mm:sszz",
            DefaultValueHandling = DefaultValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
        };

        var bulkConfig = new BulkConfig {
            BatchSize = MaxJsonBufferCount * 10,
            SetOutputIdentity = false,
            TrackingEntities = false,
            WithHoldlock = false,
            BulkCopyTimeout = 0,
        };

        using var _ = Log.Ger.BeginScope("BulkLoader");
        var chunkCnt = 0UL;
        var load = new Stopwatch();
        load.Restart();
        Log.Ger.LogInformation("starting chunk load");
        var buf = ReadSystems(serializer, lines)
            .Where(s => s.IsEligible())
            .Chunk(MaxJsonBufferCount);

        using var octx = new EEUContext(b.Options);
        var strategy = octx.Database.CreateExecutionStrategy();
        buf.ToBuffer(10)
            .ReceiveAllAsync()
            .ToBlockingEnumerable()
            .AsParallel()
            .WithDegreeOfParallelism(2)
            .ForAll(
                chunk => {
                    Log.Ger.LogDebug("processed {bytes} so far", file.Position.Bytes().Humanize());

                    var cnt = Interlocked.Increment(ref chunkCnt);
                    Log.Ger.LogInformation("ending chunk {cnt} JSON load after {elapsed}", cnt, Formatters.Humanize(load.Elapsed));
                    load.Restart();

                    strategy.Execute(
                        () => {
                            using var ctx = new EEUContext(b.Options);
                            using var tx = ctx.Database.BeginTransaction(IsolationLevel.ReadCommitted);

                            Log.Ger.LogInformation("chunk {cnt} SQL bulk loading {d}", cnt, chunk.Length);
                            // if (Log.Ger.IsEnabled(LogLevel.Trace)) {
                            //     foreach (var system in chunk) {
                            //         var sysBuf = new StringWriter();
                            //         serializer.Serialize(sysBuf, system);
                            //         Log.Ger.LogTrace("{}", sysBuf.ToString());
                            //     }
                            // }

                            // chunk.PrepareAllForUpsert();

                            chunk.PrepareAllForUpsert();
                            var mapping = chunk.GatherEntities();

                            foreach (var (t, ents) in mapping.Entities) {
                                Log.Ger.LogDebug("{cnt}: {t} had {entCnt} entities in this chunk", cnt, t.Name, ents.Count);
                                ctx.BulkInsertOrUpdate(ents, bulkConfig, null, t);
                            }

                            tx.Commit();
                        }
                    );

                    Log.Ger.LogInformation("chunk {cnt} took {time} to SQL load", cnt, Formatters.Humanize(load.Elapsed));
                    load.Restart();
                }
            );
    }

    private static IEnumerable<System> ReadSystems(
        JsonSerializer serializer,
        IEnumerable<string> source) {
        return source.AsParallel().WithDegreeOfParallelism(12)
            .Select(line => Assert.NotNull(serializer.Deserialize<System>(new JsonTextReader(new StringReader(line)))));
    }
}
