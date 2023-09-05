using LibMatrix.Homeservers;
using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Utilities.Bot.Services;

public class CommandListenerHostedService : IHostedService {
    private readonly AuthenticatedHomeserverGeneric _hs;
    private readonly ILogger<CommandListenerHostedService> _logger;
    private readonly IEnumerable<ICommand> _commands;
    private readonly LibMatrixBotConfiguration _config;

    private Task? _listenerTask;

    public CommandListenerHostedService(AuthenticatedHomeserverGeneric hs, ILogger<CommandListenerHostedService> logger, IServiceProvider services,
        LibMatrixBotConfiguration config) {
        logger.LogInformation("{} instantiated!", this.GetType().Name);
        _hs = hs;
        _logger = logger;
        _config = config;
        _logger.LogInformation("Getting commands...");
        _commands = services.GetServices<ICommand>();
        _logger.LogInformation("Got {} commands!", _commands.Count());
    }

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public Task StartAsync(CancellationToken cancellationToken) {
        _listenerTask = Run(cancellationToken);
        _logger.LogInformation("Command listener started (StartAsync)!");
        return Task.CompletedTask;
    }

    private async Task? Run(CancellationToken cancellationToken) {
        _logger.LogInformation("Starting command listener!");
        _hs.SyncHelper.TimelineEventHandlers.Add(async @event => {
            var room = await _hs.GetRoom(@event.RoomId);
            // _logger.LogInformation(eventResponse.ToJson(indent: false));
            if (@event is { Type: "m.room.message", TypedContent: RoomMessageEventData message }) {
                if (message is { MessageType: "m.text" }) {
                    var messageContentWithoutReply = message.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries).SkipWhile(x=>x.StartsWith(">")).Aggregate((x, y) => $"{x}\n{y}");
                    if (messageContentWithoutReply.StartsWith(_config.Prefix)) {
                        var command = _commands.FirstOrDefault(x => x.Name == messageContentWithoutReply.Split(' ')[0][_config.Prefix.Length..]);
                        if (command == null) {
                            await room.SendMessageEventAsync("m.room.message",
                                new RoomMessageEventData(messageType: "m.notice", body: "Command not found!"));
                            return;
                        }

                        var ctx = new CommandContext {
                            Room = room,
                            MessageEvent = @event,
                            Homeserver = _hs
                        };
                        if (await command.CanInvoke(ctx)) {
                            await command.Invoke(ctx);
                        }
                        else {
                            await room.SendMessageEventAsync("m.room.message",
                                new RoomMessageEventData(messageType: "m.notice", body: "You do not have permission to run this command!"));
                        }
                    }
                }
            }
        });
        await _hs.SyncHelper.RunSyncLoop(cancellationToken: cancellationToken);
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Shutting down command listener!");
        _listenerTask.Wait(cancellationToken);
        return Task.CompletedTask;
    }
}
