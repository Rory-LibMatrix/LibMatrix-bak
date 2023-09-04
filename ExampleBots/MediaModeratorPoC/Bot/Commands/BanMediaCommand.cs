using LibMatrix.Responses;
using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.AccountData;
using MediaModeratorPoC.Bot.Interfaces;
using MediaModeratorPoC.Bot.StateEventTypes;

namespace MediaModeratorPoC.Bot.Commands;

public class BanMediaCommand(IServiceProvider services) : ICommand {
    public string Name { get; } = "banmedia";
    public string Description { get; } = "Create a policy banning a piece of media, must be used in reply to a message";

    public async Task<bool> CanInvoke(CommandContext ctx) {
        //check if user is admin in control room
        var botData = await ctx.Homeserver.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
        var controlRoom = await ctx.Homeserver.GetRoom(botData.ControlRoom);
        var powerLevels = await controlRoom.GetPowerLevelAsync();
        var isAdmin = powerLevels.UserHasPermission(ctx.MessageEvent.Sender, "m.room.ban");
        if (!isAdmin) {
            // await ctx.Reply("You do not have permission to use this command!");
            var logRoom = await ctx.Homeserver.GetRoom(botData.LogRoom);
            await logRoom.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
                Body = $"User {ctx.MessageEvent.Sender} tried to use command {Name} but does not have permission!",
                MessageType = "m.text"
            });
        }
        return isAdmin;
    }

    public async Task Invoke(CommandContext ctx) {
        //check if reply
        if ((ctx.MessageEvent.TypedContent as RoomMessageEventData).RelatesTo is { InReplyTo: not null } ) {
            var messageContent = ctx.MessageEvent.TypedContent as RoomMessageEventData;
            try {
                var botData = await ctx.Homeserver.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
                var policyRoom = await ctx.Homeserver.GetRoom(botData.PolicyRoom);
                var logRoom = await ctx.Homeserver.GetRoom(botData.LogRoom);
                await logRoom.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
                    Body = $"User {ctx.MessageEvent.Sender} is trying to ban media {messageContent.RelatesTo!.InReplyTo!.EventId}",
                    MessageType = "m.text"
                });

                //get replied message
                var repliedMessage = await ctx.Room.GetEvent<StateEventResponse>(messageContent.RelatesTo!.InReplyTo!.EventId);

                await policyRoom.SendStateEventAsync("gay.rory.media_moderator_poc.rule.media", new MediaPolicyStateEventData() {
                    Entity = (repliedMessage.TypedContent as RoomMessageEventData).Url!,
                    Reason = string.Join(' ', ctx.Args),
                    Recommendation = PolicyRecommendationTypes.Ban
                });
            }
            catch (Exception e) {
                await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
                    Body = $"Error: {e.Message}",
                    MessageType = "m.text"
                });
            }
        }
        else {
            await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
                Body = "This command must be used in reply to a message!",
                MessageType = "m.text",
            });
        }
    }
}
