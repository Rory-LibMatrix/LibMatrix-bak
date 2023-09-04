using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ArcaneLibs.Extensions;
using LibMatrix;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using LibMatrix.Services;
using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.AccountData;
using MediaModeratorPoC.Bot.Interfaces;
using MediaModeratorPoC.Bot.StateEventTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaModeratorPoC.Bot;

public class MediaModBot : IHostedService {
    private readonly HomeserverProviderService _homeserverProviderService;
    private readonly ILogger<MediaModBot> _logger;
    private readonly MediaModBotConfiguration _configuration;
    private readonly IEnumerable<ICommand> _commands;

    private GenericRoom PolicyRoom;

    public MediaModBot(HomeserverProviderService homeserverProviderService, ILogger<MediaModBot> logger,
        MediaModBotConfiguration configuration, IServiceProvider services) {
        logger.LogInformation("MRUBot hosted service instantiated!");
        _homeserverProviderService = homeserverProviderService;
        _logger = logger;
        _configuration = configuration;
        _logger.LogInformation("Getting commands...");
        _commands = services.GetServices<ICommand>();
        _logger.LogInformation("Got {} commands!", _commands.Count());
    }

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    [SuppressMessage("ReSharper", "FunctionNeverReturns")]
    public async Task StartAsync(CancellationToken cancellationToken) {
        Directory.GetFiles("bot_data/cache").ToList().ForEach(File.Delete);
        AuthenticatedHomeserverGeneric hs;
        try {
            hs = await _homeserverProviderService.GetAuthenticatedWithToken(_configuration.Homeserver,
                _configuration.AccessToken);
        }
        catch (Exception e) {
            _logger.LogError("{}", e.Message);
            throw;
        }

        BotData botData;

        try {
            botData = await hs.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
        }
        catch (Exception e) {
            if (e is not MatrixException { ErrorCode: "M_NOT_FOUND" }) {
                _logger.LogError("{}", e.ToString());
                throw;
            }

            botData = new BotData();
            var creationContent = CreateRoomRequest.CreatePrivate(hs, name: "Media Moderator PoC - Control room", roomAliasName: "media-moderator-poc-control-room");
            creationContent.Invite = _configuration.Admins;
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.control_room";

            botData.ControlRoom = (await hs.CreateRoom(creationContent)).RoomId;

            //set access rules to allow joining via control room
            creationContent.InitialState.Add(new StateEvent {
                Type = "m.room.join_rules",
                StateKey = "",
                TypedContent = new JoinRulesEventData() {
                    JoinRule = "knock_restricted",
                    Allow = new() {
                        new JoinRulesEventData.AllowEntry() {
                            Type = "m.room_membership",
                            RoomId = botData.ControlRoom
                        }
                    }
                }
            });

            creationContent.Name = "Media Moderator PoC - Log room";
            creationContent.RoomAliasName = "media-moderator-poc-log-room";
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.log_room";
            botData.LogRoom = (await hs.CreateRoom(creationContent)).RoomId;

            creationContent.Name = "Media Moderator PoC - Policy room";
            creationContent.RoomAliasName = "media-moderator-poc-policy-room";
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.policy_room";
            botData.PolicyRoom = (await hs.CreateRoom(creationContent)).RoomId;

            await hs.SetAccountData("gay.rory.media_moderator_poc_data", botData);
        }

        PolicyRoom = await hs.GetRoom(botData.PolicyRoom);

        hs.SyncHelper.InviteReceivedHandlers.Add(async Task (args) => {
            var inviteEvent =
                args.Value.InviteState.Events.FirstOrDefault(x =>
                    x.Type == "m.room.member" && x.StateKey == hs.WhoAmI.UserId);
            _logger.LogInformation(
                $"Got invite to {args.Key} by {inviteEvent.Sender} with reason: {(inviteEvent.TypedContent as RoomMemberEventData).Reason}");
            if (inviteEvent.Sender.EndsWith(":rory.gay") || inviteEvent.Sender.EndsWith(":conduit.rory.gay")) {
                try {
                    var senderProfile = await hs.GetProfile(inviteEvent.Sender);
                    await (await hs.GetRoom(args.Key)).JoinAsync(reason: $"I was invited by {senderProfile.DisplayName ?? inviteEvent.Sender}!");
                }
                catch (Exception e) {
                    _logger.LogError("{}", e.ToString());
                    await (await hs.GetRoom(args.Key)).LeaveAsync(reason: "I was unable to join the room: " + e);
                }
            }
        });
        hs.SyncHelper.TimelineEventHandlers.Add(async @event => {
            _logger.LogInformation(
                "Got timeline event in {}: {}", @event.RoomId, @event.ToJson(indent: true, ignoreNull: true));

            var room = await hs.GetRoom(@event.RoomId);
            // _logger.LogInformation(eventResponse.ToJson(indent: false));
            if (@event is { Type: "m.room.message", TypedContent: RoomMessageEventData message }) {
                if (message is { MessageType: "m.text" } && message.Body.StartsWith(_configuration.Prefix)) {
                    var command = _commands.FirstOrDefault(x => x.Name == message.Body.Split(' ')[0][_configuration.Prefix.Length..]);
                    if (command == null) {
                        await room.SendMessageEventAsync("m.room.message",
                            new RoomMessageEventData {
                                MessageType = "m.notice",
                                Body = "Command not found!"
                            });
                        return;
                    }

                    var ctx = new CommandContext {
                        Room = room,
                        MessageEvent = @event,
                        Homeserver = hs
                    };
                    if (await command.CanInvoke(ctx)) {
                        await command.Invoke(ctx);
                    }
                    else {
                        await room.SendMessageEventAsync("m.room.message",
                            new RoomMessageEventData {
                                MessageType = "m.notice",
                                Body = "You do not have permission to run this command!"
                            });
                    }
                }
            }
        });
        await hs.SyncHelper.RunSyncLoop(cancellationToken: cancellationToken);
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Shutting down bot!");
        return Task.CompletedTask;
    }


    private async Task<bool> CheckMedia(StateEventResponse @event) {
        var stateList = PolicyRoom.GetFullStateAsync();
        await foreach (var state in stateList) {
            if(state.Type != "gay.rory.media_moderator_poc.rule.media") continue;
            var rule = state.TypedContent as MediaPolicyStateEventData;
            rule.Entity = rule.Entity.Replace("\\*", ".*").Replace("\\?", ".");
            var regex = new Regex(rule.Entity);
            if (regex.IsMatch(@event.RawContent["url"].GetValue<string>())) {
                _logger.LogInformation("{url} matched rule {rule}", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                return true;
            }
        }
        return false;
    }
}
