// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Services;
using LibMatrix.Utilities.Bot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluralContactBotPoC.Bot;

Console.WriteLine("Hello, World!");

var randomBytes = new byte[32];
Random.Shared.NextBytes(randomBytes);
var ASToken = Convert.ToBase64String(randomBytes);
Random.Shared.NextBytes(randomBytes);
var HSToken = Convert.ToBase64String(randomBytes);

var asConfig = new AppServiceConfiguration() {
    Id = "plural_contact_bot",
    Url = null,
    SenderLocalpart = "plural_contact_bot",
    AppserviceToken = ASToken,
    HomeserverToken = HSToken,
    Namespaces = new() {
        Users = new() {
            new() {
                Exclusive = false,
                Regex = "@.*"
            }
        },
        Aliases = new() {
            new() {
                Exclusive = false,
                Regex = "#.*"
            }
        },
        Rooms = new() {
            new() {
                Exclusive = false,
                Regex = "!.*"
            }
        }
    },
    RateLimited = false,
    Protocols = new List<string>() { "matrix" }
};

if (File.Exists("appservice.json"))
    asConfig = JsonSerializer.Deserialize<AppServiceConfiguration>(File.ReadAllText("appservice.json"))!;

File.WriteAllText("appservice.yaml", asConfig.ToYaml());
File.WriteAllText("appservice.json", asConfig.ToJson());
Environment.Exit(0);

var host = Host.CreateDefaultBuilder(args).ConfigureServices((_, services) => {
    services.AddScoped<TieredStorageService>(x =>
        new TieredStorageService(
            cacheStorageProvider: new FileStorageProvider("bot_data/cache/"),
            dataStorageProvider: new FileStorageProvider("bot_data/data/")
        )
    );
    services.AddSingleton<PluralContactBotConfiguration>();
    services.AddSingleton<AppServiceConfiguration>();

    services.AddRoryLibMatrixServices();
    services.AddBot(withCommands: true);

    services.AddHostedService<PluralContactBot>();
}).UseConsoleLifetime().Build();

await host.RunAsync();
