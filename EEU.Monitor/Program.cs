// See https://aka.ms/new-console-template for more information

using EliteAPI.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace EEU.Monitor;

internal static class Program {
    private static async Task Main(string[] args) {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
            .CreateLogger();

        await Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices(
                services => {
                    services.AddLogging();
                    services.AddEliteApi();
                    services.AddHostedService<PredictionService>();
                }
            )
            .Build()
            .RunAsync();

        await Log.CloseAndFlushAsync();
    }
}
