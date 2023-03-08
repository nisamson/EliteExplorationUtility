// EliteExplorationUtility - EEU.Monitor - ISystemStore.cs
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EEU.Monitor.SystemStore;

public interface ISystemStore : IHostedService {
    public Configuration Config { get; }

    public Task<Elite.System> GetSystemAsync(string address, string? systemName = null, CancellationToken cancellationToken = default);
    public Task<Elite.System> MergeSystemAsync(Elite.System system, CancellationToken cancellationToken = default);

    public Task TakeCheckpoint(CancellationToken cancellationToken = default) {
        return Task.CompletedTask;
    }

    public class Configuration {
        public const string Position = "SystemStore";

        public string DbPath { get; set; } = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            nameof(EEU),
            nameof(Monitor),
            "dataStore"
        );

        public bool WithReset { get; set; } = false;
    }
}

public static class SystemStoreExtensions {
    public static IServiceCollection AddFasterKvSystemStore(this IServiceCollection services) {
        services.AddSingleton<ISystemStore, FasterKvSystemStore>();
        return services;
    }

    public static IServiceCollection AddLiteDbSystemStore(this IServiceCollection services) {
        services.AddSingleton<ISystemStore, LiteDbSystemStore>();
        return services;
    }
}
