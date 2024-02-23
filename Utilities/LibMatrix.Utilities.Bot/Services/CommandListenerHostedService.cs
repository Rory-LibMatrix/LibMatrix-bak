using System.Reflection.Metadata;
using LibMatrix.EventTypes.Spec;
using LibMatrix.Filters;
using LibMatrix.Helpers;
using LibMatrix.Homeservers;
using LibMatrix.Utilities.Bot.Interfaces;
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
        logger.LogInformation("{} instantiated!", GetType().Name);
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
        var filter = await _hs.GetOrUploadNamedFilterIdAsync("gay.rory.libmatrix.utilities.bot.command_listener_syncfilter.dev2", new SyncFilter() {
            AccountData = new SyncFilter.EventFilter(notTypes: ["*"], limit: 1),
            Presence = new SyncFilter.EventFilter(notTypes: ["*"]),
            Room = new SyncFilter.RoomFilter() {
                AccountData = new SyncFilter.RoomFilter.StateFilter(notTypes: ["*"]),
                Ephemeral = new SyncFilter.RoomFilter.StateFilter(notTypes: ["*"]),
                State = new SyncFilter.RoomFilter.StateFilter(notTypes: ["*"]),
                Timeline = new SyncFilter.RoomFilter.StateFilter(types: ["m.room.message"], notSenders: [_hs.WhoAmI.UserId]),
            }
        });
        var syncHelper = new SyncHelper(_hs, _logger) {
            Timeout = 300_000,
            FilterId = filter
        };
        syncHelper.TimelineEventHandlers.Add(async @event => {
            try {
                var room = _hs.GetRoom(@event.RoomId);
                // _logger.LogInformation(eventResponse.ToJson(indent: false));
                if (@event is { Type: "m.room.message", TypedContent: RoomMessageEventContent message })
                    if (message is { MessageType: "m.text" }) {
                        var messageContentWithoutReply =
                            message.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries).SkipWhile(x => x.StartsWith(">")).Aggregate((x, y) => $"{x}\n{y}");
                        if (messageContentWithoutReply.StartsWith(_config.Prefix)) {
                            var command = _commands.FirstOrDefault(x => x.Name == messageContentWithoutReply.Split(' ')[0][_config.Prefix.Length..]);
                            if (command == null) {
                                await room.SendMessageEventAsync(
                                    new RoomMessageEventContent("m.notice", "Command not found!"));
                                return;
                            }

                            var ctx = new CommandContext {
                                Room = room,
                                MessageEvent = @event,
                                Homeserver = _hs
                            };

                            if (await command.CanInvoke(ctx))
                                try {
                                    await command.Invoke(ctx);
                                }
                                catch (Exception e) {
                                    await room.SendMessageEventAsync(
                                        MessageFormatter.FormatException("An error occurred during the execution of this command", e));
                                }
                            else
                                await room.SendMessageEventAsync(
                                    new RoomMessageEventContent("m.notice", "You do not have permission to run this command!"));
                        }
                    }
            }
            catch (Exception e) {
                _logger.LogError(e, "Error in command listener!");
            }
        });
        await syncHelper.RunSyncLoopAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Shutting down command listener!");
        if (_listenerTask is null) {
            _logger.LogError("Could not shut down command listener task because it was null!");
            return;
        }

        await _listenerTask.WaitAsync(cancellationToken);
    }
}