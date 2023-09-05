using System.Security.Cryptography;
using ArcaneLibs.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Responses;
using LibMatrix.Services;
using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.AccountData;
using MediaModeratorPoC.Bot.Interfaces;
using MediaModeratorPoC.Bot.StateEventTypes;

namespace MediaModeratorPoC.Bot.Commands;

public class BanMediaCommand(IServiceProvider services, HomeserverProviderService hsProvider, HomeserverResolverService hsResolver) : ICommand {
    public string Name { get; } = "banmedia";
    public string Description { get; } = "Create a policy banning a piece of media, must be used in reply to a message";

    public async Task<bool> CanInvoke(CommandContext ctx) {
        //check if user is admin in control room
        var botData = await ctx.Homeserver.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
        var controlRoom = await ctx.Homeserver.GetRoom(botData.ControlRoom);
        var isAdmin = (await controlRoom.GetPowerLevelsAsync())!.UserHasPermission(ctx.MessageEvent.Sender, "m.room.ban");
        if (!isAdmin) {
            // await ctx.Reply("You do not have permission to use this command!");
            await (await ctx.Homeserver.GetRoom(botData.LogRoom!)).SendMessageEventAsync("m.room.message",
                new RoomMessageEventData(body: $"User {ctx.MessageEvent.Sender} tried to use command {Name} but does not have permission!", messageType: "m.text"));
        }

        return isAdmin;
    }

    public async Task Invoke(CommandContext ctx) {
        var botData = await ctx.Homeserver.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
        var policyRoom = await ctx.Homeserver.GetRoom(botData.PolicyRoom ?? botData.ControlRoom);
        var logRoom = await ctx.Homeserver.GetRoom(botData.LogRoom ?? botData.ControlRoom);

        //check if reply
        var messageContent = ctx.MessageEvent.TypedContent as RoomMessageEventData;
        if (messageContent?.RelatesTo is { InReplyTo: not null }) {
            try {
                await logRoom.SendMessageEventAsync("m.room.message",
                    new RoomMessageEventData(
                        body: $"User {MessageFormatter.HtmlFormatMention(ctx.MessageEvent.Sender)} is trying to ban media {messageContent!.RelatesTo!.InReplyTo!.EventId}",
                        messageType: "m.text"));

                //get replied message
                var repliedMessage = await ctx.Room.GetEvent<StateEventResponse>(messageContent.RelatesTo!.InReplyTo!.EventId);

                //check if recommendation is in list
                if (ctx.Args.Length < 2) {
                    await ctx.Room.SendMessageEventAsync("m.room.message", MessageFormatter.FormatError("You must specify a recommendation type and reason!"));
                    return;
                }

                var recommendation = ctx.Args[0];

                if (recommendation is not ("ban" or "kick" or "mute" or "redact" or "spoiler" or "warn" or "warn_admins")) {
                    await ctx.Room.SendMessageEventAsync("m.room.message", MessageFormatter.FormatError($"Invalid recommendation type {recommendation}, must be `warn_admins`, `warn`, `spoiler`, `redact`, `mute`, `kick` or `ban`!"));
                    return;
                }



                //hash file
                var mxcUri = (repliedMessage.TypedContent as RoomMessageEventData).Url!;
                var resolvedUri = await hsResolver.ResolveMediaUri(mxcUri.Split('/')[2], mxcUri);
                var hashAlgo = SHA3_256.Create();
                var uriHash = hashAlgo.ComputeHash(mxcUri.AsBytes().ToArray());
                byte[]? fileHash = null;

                try {
                    fileHash = await hashAlgo.ComputeHashAsync(await ctx.Homeserver._httpClient.GetStreamAsync(resolvedUri));
                }
                catch (Exception ex) {
                    await logRoom.SendMessageEventAsync("m.room.message",
                        MessageFormatter.FormatException($"Error calculating file hash for {mxcUri} via {mxcUri.Split('/')[2]}, retrying via {ctx.Homeserver.HomeServerDomain}...",
                            ex));
                    try {
                        resolvedUri = await hsResolver.ResolveMediaUri(ctx.Homeserver.HomeServerDomain, mxcUri);
                        fileHash = await hashAlgo.ComputeHashAsync(await ctx.Homeserver._httpClient.GetStreamAsync(resolvedUri));
                    }
                    catch (Exception ex2) {
                        await ctx.Room.SendMessageEventAsync("m.room.message", MessageFormatter.FormatException("Error calculating file hash", ex2));
                        await logRoom.SendMessageEventAsync("m.room.message",
                            MessageFormatter.FormatException($"Error calculating file hash via {ctx.Homeserver.HomeServerDomain}!", ex2));
                    }
                }

                MediaPolicyStateEventData policy;
                await policyRoom.SendStateEventAsync("gay.rory.media_moderator_poc.rule.media", Guid.NewGuid().ToString(), policy = new MediaPolicyStateEventData {
                    Entity = uriHash,
                    FileHash = fileHash,
                    Reason = string.Join(' ', ctx.Args[1..]),
                    Recommendation = recommendation,
                });

                await ctx.Room.SendMessageEventAsync("m.room.message", MessageFormatter.FormatSuccessJson("Media policy created", policy));
                await logRoom.SendMessageEventAsync("m.room.message", MessageFormatter.FormatSuccessJson("Media policy created", policy));
            }
            catch (Exception e) {
                await logRoom.SendMessageEventAsync("m.room.message", MessageFormatter.FormatException("Error creating policy", e));
                await ctx.Room.SendMessageEventAsync("m.room.message", MessageFormatter.FormatException("Error creating policy", e));
                await using var stream = new MemoryStream(e.ToString().AsBytes().ToArray());
                await logRoom.SendFileAsync("m.file", "error.log.cs", stream);
            }
        }
        else {
            await ctx.Room.SendMessageEventAsync("m.room.message", MessageFormatter.FormatError("This command must be used in reply to a message!"));
        }
    }
}
