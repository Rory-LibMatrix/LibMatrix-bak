using ArcaneLibs;
using LibMatrix.Homeservers;
using LibMatrix.Services;
using LibMatrix.Utilities.Bot.Interfaces;
using LibMatrix.Utilities.Bot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibMatrix.Utilities.Bot;

public static class BotCommandInstaller {
    public static IServiceCollection AddBotCommands(this IServiceCollection services) {
        foreach (var commandClass in new ClassCollector<ICommand>().ResolveFromAllAccessibleAssemblies()) {
            Console.WriteLine($"Adding command {commandClass.Name}");
            services.AddScoped(typeof(ICommand), commandClass);
        }

        return services;
    }

    public static IServiceCollection AddBot(this IServiceCollection services, bool withCommands = true, bool isAppservice = false) {
        services.AddSingleton<LibMatrixBotConfiguration>();

        services.AddScoped<AuthenticatedHomeserverGeneric>(x => {
            var config = x.GetService<LibMatrixBotConfiguration>() ?? throw new Exception("No configuration found!");
            var hsProvider = x.GetService<HomeserverProviderService>() ?? throw new Exception("No homeserver provider found!");
            var hs = hsProvider.GetAuthenticatedWithToken(config.Homeserver, config.AccessToken).Result;

            return hs;
        });

        if (withCommands) {
            Console.WriteLine("Adding command handler...");
            services.AddBotCommands();
            services.AddHostedService<CommandListenerHostedService>();
            // services.AddSingleton<IHostedService, CommandListenerHostedService>();
        }

        return services;
    }
}