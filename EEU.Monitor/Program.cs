// See https://aka.ms/new-console-template for more information

using EEU.Monitor.Prediction;
using EEU.Monitor.SystemStore;
using EliteAPI.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.Json;
using Serilog;
using Serilog.Core;

namespace EEU.Monitor;

internal static class Program {
    private static async Task Main(string[] args) {
        await Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
            )
            .ConfigureServices(
                services => {
                    services.AddLogging();
                    services.AddEliteApi();
                    services.AddLiteDbSystemStore();
                    services.AddHostedService<PredictionService>();
                }
            )
            .Build()
            .RunAsync();

        await Log.CloseAndFlushAsync();
    }
}
