using Microsoft.Extensions.DependencyInjection;

namespace LibMatrix.Services;

public static class ServiceInstaller {

    public static IServiceCollection AddRoryLibMatrixServices(this IServiceCollection services, RoryLibMatrixConfiguration? config = null) {
        //Check required services
        if (!services.Any(x => x.ServiceType == typeof(TieredStorageService)))
            throw new Exception("[MRUCore/DI] No TieredStorageService has been registered!");
        //Add config
        services.AddSingleton(config ?? new RoryLibMatrixConfiguration());

        //Add services
        services.AddSingleton<HomeserverResolverService>();

        if (services.First(x => x.ServiceType == typeof(TieredStorageService)).Lifetime == ServiceLifetime.Singleton) {
            services.AddSingleton<HomeserverProviderService>();
        }
        else {
            services.AddScoped<HomeserverProviderService>();
        }

        // services.AddScoped<MatrixHttpClient>();
        return services;
    }


}

public class RoryLibMatrixConfiguration {
    public string AppName { get; set; } = "Rory&::LibMatrix";
}
