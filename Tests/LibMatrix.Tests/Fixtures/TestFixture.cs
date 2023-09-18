using ArcaneLibs.Extensions;
using LibMatrix.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Fixtures;

public class TestFixture : TestBedFixture {
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration) {
        services.AddSingleton<TieredStorageService>(x =>
            new TieredStorageService(
                cacheStorageProvider: null,
                dataStorageProvider: null
            )
        );

        services.AddRoryLibMatrixServices();

        services.AddSingleton<Config>(config => {
            var conf = new Config();
            configuration?.GetSection("Configuration").Bind(conf);

            File.WriteAllText("configuration.json", conf.ToJson());

            return conf;
        });
    }

    protected override ValueTask DisposeAsyncCore()
        => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings() {
        yield return new TestAppSettings { Filename = "appsettings.json", IsOptional = true };
    }
}
