// See https://aka.ms/new-console-template for more information

using ArcaneLibs;
using LibMatrix.Services;
using MediaModeratorPoC.Bot;
using MediaModeratorPoC.Bot.Interfaces;
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
    foreach (var commandClass in new ClassCollector<ICommand>().ResolveFromAllAccessibleAssemblies()) {
        Console.WriteLine($"Adding command {commandClass.Name}");
        services.AddScoped(typeof(ICommand), commandClass);
    }

    services.AddHostedService<MediaModBot>();
}).UseConsoleLifetime().Build();

await host.RunAsync();
