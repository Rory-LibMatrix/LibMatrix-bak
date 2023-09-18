// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.Services;
using LibMatrix.Utilities.Bot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluralContactBotPoC;
using PluralContactBotPoC.Bot;

Console.WriteLine("Hello, World!");

var host = Host.CreateDefaultBuilder(args).ConfigureServices((_, services) => {
    services.AddScoped<TieredStorageService>(x =>
        new TieredStorageService(
            cacheStorageProvider: new FileStorageProvider("bot_data/cache/"),
            dataStorageProvider: new FileStorageProvider("bot_data/data/")
        )
    );
    // services.AddSingleton<DataFetcherConfiguration>();
    services.AddSingleton<AppServiceConfiguration>();

    services.AddRoryLibMatrixServices();
    services.AddBot(withCommands: false);

    services.AddHostedService<DataFetcher>();
}).UseConsoleLifetime().Build();

await host.RunAsync();
