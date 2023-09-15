using ArcaneLibs;
using ArcaneLibs.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Homeservers;
using LibMatrix.Services;
using LibMatrix.StateEventTypes.Spec;
using LibMatrix.Utilities.Bot.Services;
using MediaModeratorPoC.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var config = x.GetService<LibMatrixBotConfiguration>();
            var hsProvider = x.GetService<HomeserverProviderService>();
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
