using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EEU.Monitor.SystemStore;

public interface ISystemStore : IHostedService {
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

    public Configuration Config { get; }

    public Task<Elite.System> GetSystemAsync(string address, string? systemName = null, CancellationToken cancellationToken = default);
    public Task<Elite.System> MergeSystemAsync(Elite.System system, CancellationToken cancellationToken = default);

    public Task TakeCheckpoint(CancellationToken cancellationToken = default) {
        return Task.CompletedTask;
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
