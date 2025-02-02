using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public static class ServiceInstaller {
    public static IServiceCollection AddRoryLibMatrixServices(this IServiceCollection services, RoryLibMatrixConfiguration? config = null) {
        //Check required services
        // if (!services.Any(x => x.ServiceType == typeof(TieredStorageService)))
        // throw new Exception("[RMUCore/DI] No TieredStorageService has been registered!");
        //Add config
        services.AddSingleton(config ?? new RoryLibMatrixConfiguration());

        //Add services
        services.AddSingleton<HomeserverResolverService>(sp => new HomeserverResolverService(sp.GetRequiredService<ILogger<HomeserverResolverService>>()));

        // if (services.First(x => x.ServiceType == typeof(TieredStorageService)).Lifetime == ServiceLifetime.Singleton) {
        services.AddSingleton<HomeserverProviderService>();
        // }
        // else {
        // services.AddScoped<HomeserverProviderService>();
        // }

        // services.AddScoped<MatrixHttpClient>();
        return services;
    }
}

public class RoryLibMatrixConfiguration {
    public string AppName { get; set; } = "Rory&::LibMatrix";
}