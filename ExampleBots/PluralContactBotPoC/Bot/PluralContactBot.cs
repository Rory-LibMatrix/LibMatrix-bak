using System.Text;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Helpers;
using LibMatrix.Homeservers;
using LibMatrix.RoomTypes;
using LibMatrix.Services;
using LibMatrix.Utilities.Bot;
using LibMatrix.Utilities.Bot.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluralContactBotPoC.Bot.AccountData;
using PluralContactBotPoC.Bot.StateEventTypes;

namespace PluralContactBotPoC.Bot;

public class PluralContactBot(AuthenticatedHomeserverGeneric hs, ILogger<PluralContactBot> logger, LibMatrixBotConfiguration botConfiguration,
    PluralContactBotConfiguration configuration,
    HomeserverResolverService hsResolver) : IHostedService {
    private readonly IEnumerable<ICommand> _commands;

    private Task _listenerTask;

    private GenericRoom? _logRoom;

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken) {
        _listenerTask = Run(cancellationToken);
        logger.LogInformation("Bot started!");
    }

    private async Task Run(CancellationToken cancellationToken) {
        Directory.GetFiles("bot_data/cache").ToList().ForEach(File.Delete);

        BotData botData;

        _logRoom = hs.GetRoom(botConfiguration.LogRoom);

        hs.SyncHelper.InviteReceivedHandlers.Add(async Task (args) => {
            var inviteEvent =
                args.Value.InviteState.Events.FirstOrDefault(x =>
                    x.Type == "m.room.member" && x.StateKey == hs.UserId);
            logger.LogInformation("Got invite to {} by {} with reason: {}", args.Key, inviteEvent.Sender, (inviteEvent.TypedContent as RoomMemberEventContent).Reason);

            try {
                var accountData = await hs.GetAccountData<SystemData>($"gay.rory.plural_contact_bot.system_data#{inviteEvent.StateKey}");
                if (accountData.Members.Contains(inviteEvent.Sender)) {
                    await (hs.GetRoom(args.Key)).JoinAsync(reason: "I was invited by a system member!");

                    await _logRoom.SendMessageEventAsync(
                        MessageFormatter.FormatSuccess(
                            $"I was invited by a system member ({MessageFormatter.HtmlFormatMention(inviteEvent.Sender)}) to {MessageFormatter.HtmlFormatMention(args.Key)}"));

                    return;
                }
            }
            catch (Exception e) {
                await _logRoom.SendMessageEventAsync(
                    MessageFormatter.FormatException(
                        $"Exception handling event {inviteEvent.EventId} by {inviteEvent.Sender} in {MessageFormatter.HtmlFormatMention(inviteEvent.RoomId)}", e));
            }

            if (inviteEvent.Sender.EndsWith(":rory.gay") || inviteEvent.Sender.EndsWith(":conduit.rory.gay")) {
                try {
                    var senderProfile = await hs.GetProfileAsync(inviteEvent.Sender);
                    await (hs.GetRoom(args.Key)).JoinAsync(reason: $"I was invited by {senderProfile.DisplayName ?? inviteEvent.Sender}!");
                }
                catch (Exception e) {
                    logger.LogError("{}", e.ToString());
                    await (hs.GetRoom(args.Key)).LeaveAsync(reason: "I was unable to join the room: " + e);
                }
            }
        });

        hs.SyncHelper.TimelineEventHandlers.Add(async @event => {
            var room = hs.GetRoom(@event.RoomId);
            try {
                logger.LogInformation(
                    "Got timeline event in {}: {}", @event.RoomId, @event.ToJson(indent: true, ignoreNull: true));

                if (@event is { Type: "m.room.message", TypedContent: RoomMessageEventContent message }) { }
            }
            catch (Exception e) {
                logger.LogError("{}", e.ToString());
                await _logRoom.SendMessageEventAsync(
                    MessageFormatter.FormatException($"Exception handling event {@event.EventId} by {@event.Sender} in {MessageFormatter.HtmlFormatMention(room.RoomId)}", e));
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(e.ToString()));
                await _logRoom.SendFileAsync("m.file", "error.log.cs", stream);
            }
        });
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Shutting down bot!");
    }
}
