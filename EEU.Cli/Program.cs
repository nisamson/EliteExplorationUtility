// See https://aka.ms/new-console-template for more information

// ReSharper disable CheckNamespace

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EEU.Model;
using EEU.Model.Biology;
using EEU.Utils;
using EFCore.BulkExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace EEU.Cli;

internal static class Program {
    private class Options { }

    private static void Main(string[] args) {
        var loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole((c) => { c.IncludeScopes = true; })
                .SetMinimumLevel(LogLevel.Trace)
        );
        var dbOptions = new DbContextOptionsBuilder<EEUContext>()
            .UseSqlServer(EEUContext.DefaultConnString);
        // .EnableSensitiveDataLogging()
        // .UseLoggerFactory(loggerFactory);


        var logger = loggerFactory.CreateLogger("EEU");
        Log.Logger.BackingLogger = logger;

        LoadCsvs(dbOptions);
        // LoadJson(dbOptions);

        // using var fs = new FileStream(@"F:\Big Downloads\galaxy.json", FileMode.Open);
        // // try {
        // Loader.BulkUpsertFromFile(dbOptions, fs, 158 * 1024 * 1024 * 1024L);
        // // } catch (Exception e) {
        // //     Console.WriteLine(e);
        // //     Console.Out.Flush();
        // //     Console.Error.Flush();
        // //     throw;
        // // }
    }

    private static void LoadCsvs(DbContextOptionsBuilder<EEUContext> opts) {
        var path = $@"C:\Users\{Environment.UserName}\Documents\SignalsData";
        var bulkConfig = new BulkConfig {
            BatchSize = 10000,
        };
        // {
        //     using var ctx = new EEUContext(opts.Options);
        //     ctx.Biology.BatchDelete();
        // }
        //
        // foreach (var chunk in Biology.FromCsvDirectory(path).Chunk(1024 * 10)) {
        //     chunk.PrepareAllForUpsert();
        //     var ents = chunk.GatherEntities();
        //     using var ctx = new EEUContext(opts.Options);
        //     var entsDirect = ents.Entities
        //         .SelectMany(x => x.Item2)
        //         .ToList();
        //     ctx.BulkInsertOrUpdate(entsDirect, bulkConfig, null, typeof(Biology));
        // }

        using var infoReader = new CsvReader(
            new CsvParser(
                new StreamReader(new FileStream($@"C:\Users\{Environment.UserName}\Documents\AccurateValues.csv", FileMode.Open)),
                new CsvConfiguration(CultureInfo.CurrentCulture) {
                    TrimOptions = TrimOptions.Trim,
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                }
            )
        )!;
        {
            using var ctx = new EEUContext(opts.Options);
            ctx.BulkInsertOrUpdateOrDelete(
                SpeciesInformation.FromLimitedCsv(infoReader).ToList(),
                bulkConfig,
                null,
                typeof(SpeciesInformation)
            );
        }
    }

    private static void LoadJson(DbContextOptionsBuilder<EEUContext> opts) {
        var path = $@"C:\Users\{Environment.UserName}\Downloads\codex.lines";
        using var rd = new StreamReader(new FileStream(path, FileMode.Open));
        using var ctx = new EEUContext(opts.Options);
        ctx.LoadCodexJson(rd);
    }
}
