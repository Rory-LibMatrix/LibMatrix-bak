// See https://aka.ms/new-console-template for more information

using LibMatrix.Services;
using LibMatrix.Utilities.Bot;
using MediaModeratorPoC.Bot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Hello, World!");

var host = Host.CreateDefaultBuilder(args).ConfigureServices((_, services) => {
    services.AddScoped<TieredStorageService>(x =>
        new TieredStorageService(
            cacheStorageProvider: new FileStorageProvider("bot_data/cache/"),
            dataStorageProvider: new FileStorageProvider("bot_data/data/")
        )
    );
    services.AddSingleton<MediaModBotConfiguration>();

    services.AddRoryLibMatrixServices();
    services.AddBot(withCommands: true);

    services.AddHostedService<MediaModBot>();
}).UseConsoleLifetime().Build();

await host.RunAsync();
