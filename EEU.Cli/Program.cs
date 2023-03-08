// EliteExplorationUtility - EEU.Cli - Program.cs
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

// ReSharper disable CheckNamespace


using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EEU.Model;
using EEU.Model.Biology;
using EEU.Utils;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EEU.Cli;

internal static class Program {
    // todo: add a command line parser so we don't need to hardcode everything
    private static void Main(string[] args) {
        var loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(c => { c.IncludeScopes = true; })
                .SetMinimumLevel(LogLevel.Trace)
        );
        var dbOptions = new DbContextOptionsBuilder<EEUContext>()
            .UseSqlServer(EEUContext.DefaultConnString);
        // .EnableSensitiveDataLogging()
        // .UseLoggerFactory(loggerFactory);


        var logger = loggerFactory.CreateLogger("EEU");
        Log.Logger.BackingLogger = logger;

        // LoadCsvs(dbOptions);
        // LoadJson(dbOptions);

        using var fs = new FileStream(@"F:\Big Downloads\galaxy.json", FileMode.Open);
        // try {
        Loader.BulkUpsertFromFile(dbOptions, fs, 158 * 1024 * 1024 * 1024L);
        // } catch (Exception e) {
        //     Console.WriteLine(e);
        //     Console.Out.Flush();
        //     Console.Error.Flush();
        //     throw;
        // }
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

    private class Options { }
}
