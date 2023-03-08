// EliteExplorationUtility - EEU.Monitor - Program.cs
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

using EEU.Monitor.Prediction;
using EEU.Monitor.SystemStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
