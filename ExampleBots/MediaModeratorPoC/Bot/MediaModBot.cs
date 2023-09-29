using System.Buffers.Text;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using ArcaneLibs.Extensions;
using LibMatrix;
using LibMatrix.EventTypes.Spec;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Helpers;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using LibMatrix.Services;
using LibMatrix.Utilities.Bot.Interfaces;
using MediaModeratorPoC.Bot.AccountData;
using MediaModeratorPoC.Bot.StateEventTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaModeratorPoC.Bot;

public class MediaModBot(AuthenticatedHomeserverGeneric hs, ILogger<MediaModBot> logger, MediaModBotConfiguration configuration,
    HomeserverResolverService hsResolver) : IHostedService {
    private readonly IEnumerable<ICommand> _commands;

    private Task _listenerTask;

    private GenericRoom _policyRoom;
    private GenericRoom _logRoom;
    private GenericRoom _controlRoom;

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken) {
        _listenerTask = Run(cancellationToken);
        logger.LogInformation("Bot started!");
    }

    private async Task Run(CancellationToken cancellationToken) {
        Directory.GetFiles("bot_data/cache").ToList().ForEach(File.Delete);

        BotData botData;

        try {
            botData = await hs.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
        }
        catch (Exception e) {
            if (e is not MatrixException { ErrorCode: "M_NOT_FOUND" }) {
                logger.LogError("{}", e.ToString());
                throw;
            }

            botData = new BotData();
            var creationContent = CreateRoomRequest.CreatePrivate(hs, name: "Media Moderator PoC - Control room", roomAliasName: "media-moderator-poc-control-room");
            creationContent.Invite = configuration.Admins;
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.control_room";

            botData.ControlRoom = (await hs.CreateRoom(creationContent)).RoomId;

            //set access rules to allow joining via control room
            creationContent.InitialState.Add(new StateEvent {
                Type = "m.room.join_rules",
                StateKey = "",
                TypedContent = new JoinRulesEventContent {
                    JoinRule = "knock_restricted",
                    Allow = new() {
                        new JoinRulesEventContent.AllowEntry {
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

        _policyRoom = hs.GetRoom(botData.PolicyRoom ?? botData.ControlRoom);
        _logRoom = hs.GetRoom(botData.LogRoom ?? botData.ControlRoom);
        _controlRoom = hs.GetRoom(botData.ControlRoom);

        List<string> admins = new();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () => {
            while (!cancellationToken.IsCancellationRequested) {
                var controlRoomMembers = _controlRoom.GetMembersAsync();
                await foreach (var member in controlRoomMembers) {
                    if ((member.TypedContent as RoomMemberEventContent).Membership == "join") admins.Add(member.UserId);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }, cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        hs.SyncHelper.InviteReceivedHandlers.Add(async Task (args) => {
            var inviteEvent =
                args.Value.InviteState.Events.FirstOrDefault(x =>
                    x.Type == "m.room.member" && x.StateKey == hs.UserId);
            logger.LogInformation(
                $"Got invite to {args.Key} by {inviteEvent.Sender} with reason: {(inviteEvent.TypedContent as RoomMemberEventContent).Reason}");
            if (inviteEvent.Sender.EndsWith(":rory.gay") || inviteEvent.Sender.EndsWith(":conduit.rory.gay")) {
                try {
                    var senderProfile = await hs.GetProfileAsync(inviteEvent.Sender);
                    await hs.GetRoom(args.Key).JoinAsync(reason: $"I was invited by {senderProfile.DisplayName ?? inviteEvent.Sender}!");
                }
                catch (Exception e) {
                    logger.LogError("{}", e.ToString());
                    await hs.GetRoom(args.Key).LeaveAsync(reason: "I was unable to join the room: " + e);
                }
            }
        });

        hs.SyncHelper.TimelineEventHandlers.Add(async @event => {
            var room = hs.GetRoom(@event.RoomId);
            try {
                logger.LogInformation(
                    "Got timeline event in {}: {}", @event.RoomId, @event.ToJson(indent: true, ignoreNull: true));

                if (@event is { Type: "m.room.message", TypedContent: RoomMessageEventContent message }) {
                    if (message is { MessageType: "m.image" }) {
                        //check media
                        var matchedPolicy = await CheckMedia(@event);
                        if (matchedPolicy is null) return;
                        var matchedpolicyData = matchedPolicy.TypedContent as MediaPolicyEventContent;
                        var recommendation = matchedpolicyData.Recommendation;
                        await _logRoom.SendMessageEventAsync(
                            new RoomMessageEventContent(
                                body:
                                $"User {MessageFormatter.HtmlFormatMention(@event.Sender)} posted an image in {MessageFormatter.HtmlFormatMention(room.RoomId)} that matched rule {matchedPolicy.StateKey}, applying action {matchedpolicyData.Recommendation}, as described in rule: {matchedPolicy.RawContent!.ToJson(ignoreNull: true)}",
                                messageType: "m.text") {
                                Format = "org.matrix.custom.html",
                                FormattedBody =
                                    $"<font color=\"#FFFF00\">User {MessageFormatter.HtmlFormatMention(@event.Sender)} posted an image in {MessageFormatter.HtmlFormatMention(room.RoomId)} that matched rule {matchedPolicy.StateKey}, applying action {matchedpolicyData.Recommendation}, as described in rule: <pre>{matchedPolicy.RawContent!.ToJson(ignoreNull: true)}</pre></font>"
                            });
                        switch (recommendation) {
                            case "warn_admins": {
                                await _controlRoom.SendMessageEventAsync(
                                    new RoomMessageEventContent(
                                        body: $"{string.Join(' ', admins)}\nUser {MessageFormatter.HtmlFormatMention(@event.Sender)} posted a banned image {message.Url}",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody = $"{string.Join(' ', admins.Select(u => MessageFormatter.HtmlFormatMention(u)))}\n" +
                                                        $"<font color=\"#FF0000\">User {MessageFormatter.HtmlFormatMention(@event.Sender)} posted a banned image <a href=\"{message.Url}\">{message.Url}</a></font>"
                                    });
                                break;
                            }
                            case "warn": {
                                await room.SendMessageEventAsync(
                                    new RoomMessageEventContent(
                                        body: $"Please be careful when posting this image: {matchedpolicyData.Reason ?? "No reason specified"}",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody =
                                            $"<font color=\"#FFFF00\">Please be careful when posting this image: {matchedpolicyData.Reason ?? "No reason specified"}</a></font>"
                                    });
                                break;
                            }
                            case "redact": {
                                await room.RedactEventAsync(@event.EventId, matchedpolicyData.Reason ?? "No reason specified");
                                break;
                            }
                            case "spoiler": {
                                // <blockquote>
                                //  <a href=\"https://matrix.to/#/@emma:rory.gay\">@emma:rory.gay</a><br>
                                //  <a href=\"https://codeberg.org/crimsonfork/CN\"></a>
                                //  <font color=\"#dc143c\" data-mx-color=\"#dc143c\">
                                //      <b>CN</b>
                                //  </font>:
                                //  <a href=\"https://the-apothecary.club/_matrix/media/v3/download/rory.gay/sLkdxUhipiQaFwRkXcPSRwdg\">test</a><br>
                                //  <span data-mx-spoiler=\"\"><a href=\"https://the-apothecary.club/_matrix/media/v3/download/rory.gay/sLkdxUhipiQaFwRkXcPSRwdg\">
                                //      <img src=\"mxc://rory.gay/sLkdxUhipiQaFwRkXcPSRwdg\" height=\"69\"></a>
                                //  </span>
                                // </blockquote>
                                await room.SendMessageEventAsync(
                                    new RoomMessageEventContent(
                                        body:
                                        $"Please be careful when posting this image: {matchedpolicyData.Reason}, I have spoilered it for you:",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody =
                                            $"<font color=\"#FFFF00\">Please be careful when posting this image: {matchedpolicyData.Reason}, I have spoilered it for you:</a></font>"
                                    });
                                var imageUrl = message.Url;
                                await room.SendMessageEventAsync(
                                    new RoomMessageEventContent(body: $"CN: {imageUrl}",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody = $"""
                                                             <blockquote>
                                                                <font color=\"#dc143c\" data-mx-color=\"#dc143c\">
                                                                    <b>CN</b>
                                                                </font>:
                                                                <a href=\"{imageUrl}\">{matchedpolicyData.Reason}</a><br>
                                                                <span data-mx-spoiler=\"\">
                                                                    <a href=\"{imageUrl}\">
                                                                        <img src=\"{imageUrl}\" height=\"69\">
                                                                    </a>
                                                                </span>
                                                             </blockquote>
                                                         """
                                    });
                                await room.RedactEventAsync(@event.EventId, "Automatically spoilered: " + matchedpolicyData.Reason);
                                break;
                            }
                            case "mute": {
                                await room.RedactEventAsync(@event.EventId, matchedpolicyData.Reason);
                                //change powerlevel to -1
                                var currentPls = await room.GetPowerLevelsAsync();
                                if(currentPls is null) {
                                    logger.LogWarning("Unable to get power levels for {room}", room.RoomId);
                                    await _logRoom.SendMessageEventAsync(
                                        MessageFormatter.FormatError($"Unable to get power levels for {MessageFormatter.HtmlFormatMention(room.RoomId)}"));
                                    return;
                                }
                                currentPls.Users ??= new();
                                currentPls.Users[@event.Sender] = -1;
                                await room.SendStateEventAsync("m.room.power_levels", currentPls);
                                break;
                            }
                            case "kick": {
                                await room.RedactEventAsync(@event.EventId, matchedpolicyData.Reason);
                                await room.KickAsync(@event.Sender, matchedpolicyData.Reason);
                                break;
                            }
                            case "ban": {
                                await room.RedactEventAsync(@event.EventId, matchedpolicyData.Reason);
                                await room.BanAsync(@event.Sender, matchedpolicyData.Reason);
                                break;
                            }
                            default: {
                                throw new ArgumentOutOfRangeException("recommendation",
                                    $"Unknown response type {matchedpolicyData.Recommendation}!");
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                logger.LogError("{}", e.ToString());
                await _controlRoom.SendMessageEventAsync(
                    MessageFormatter.FormatException($"Unable to ban user in {MessageFormatter.HtmlFormatMention(room.RoomId)}", e));
                await _logRoom.SendMessageEventAsync(
                    MessageFormatter.FormatException($"Unable to ban user in {MessageFormatter.HtmlFormatMention(room.RoomId)}", e));
                await using var stream = new MemoryStream(e.ToString().AsBytes().ToArray());
                await _controlRoom.SendFileAsync("m.file", "error.log.cs", stream);
                await _logRoom.SendFileAsync("m.file", "error.log.cs", stream);
            }
        });
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Shutting down bot!");
    }

    private async Task<StateEventResponse?> CheckMedia(StateEventResponse @event) {
        var stateList = _policyRoom.GetFullStateAsync();
        var hashAlgo = SHA3_256.Create();

        var mxcUri = @event.RawContent["url"].GetValue<string>();
        var resolvedUri = await hsResolver.ResolveMediaUri(mxcUri.Split('/')[2], mxcUri);
        var uriHash = hashAlgo.ComputeHash(mxcUri.AsBytes().ToArray());
        byte[]? fileHash = null;

        try {
            fileHash = await hashAlgo.ComputeHashAsync(await hs._httpClient.GetStreamAsync(resolvedUri));
        }
        catch (Exception ex) {
            await _logRoom.SendMessageEventAsync(
                MessageFormatter.FormatException($"Error calculating file hash for {mxcUri} via {mxcUri.Split('/')[2]} ({resolvedUri}), retrying via {hs.HomeServerDomain}...",
                    ex));
            try {
                resolvedUri = await hsResolver.ResolveMediaUri(hs.HomeServerDomain, mxcUri);
                fileHash = await hashAlgo.ComputeHashAsync(await hs._httpClient.GetStreamAsync(resolvedUri));
            }
            catch (Exception ex2) {
                await _logRoom.SendMessageEventAsync(
                    MessageFormatter.FormatException($"Error calculating file hash via {hs.HomeServerDomain} ({resolvedUri})!", ex2));
            }
        }

        logger.LogInformation("Checking media {url} with hash {hash}", resolvedUri, fileHash);

        await foreach (var state in stateList) {
            if (state.Type != "gay.rory.media_moderator_poc.rule.media" && state.Type != "gay.rory.media_moderator_poc.rule.server") continue;
            if (!state.RawContent.ContainsKey("entity")) {
                logger.LogWarning("Rule {rule} has no entity, this event was probably redacted!", state.StateKey);
                continue;
            }

            logger.LogInformation("Checking rule {rule}: {data}", state.StateKey, state.TypedContent.ToJson(ignoreNull: true, indent: false));
            var rule = state.TypedContent as MediaPolicyEventContent;
            if (state.Type == "gay.rory.media_moderator_poc.rule.server" && rule.ServerEntity is not null) {
                rule.ServerEntity = rule.ServerEntity.Replace("\\*", ".*").Replace("\\?", ".");
                var regex = new Regex($"mxc://({rule.ServerEntity})/.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (regex.IsMatch(@event.RawContent["url"]!.GetValue<string>())) {
                    logger.LogInformation("{url} matched rule {rule}", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                    return state;
                }
            }

            if (rule.Entity is not null && uriHash.SequenceEqual(rule.Entity)) {
                logger.LogInformation("{url} matched rule {rule} by uri hash", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                return state;
            }

            logger.LogInformation("uri hash {uriHash} did not match rule's {ruleUriHash}", Convert.ToBase64String(uriHash), Convert.ToBase64String(rule.Entity));

            if (rule.FileHash is not null && fileHash is not null && rule.FileHash.SequenceEqual(fileHash)) {
                logger.LogInformation("{url} matched rule {rule} by file hash", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                return state;
            }

            logger.LogInformation("file hash {fileHash} did not match rule's {ruleFileHash}", Convert.ToBase64String(fileHash), Convert.ToBase64String(rule.FileHash));

            //check pixels every 10% of the way through the image using ImageSharp
            // var image = Image.Load(await _hs._httpClient.GetStreamAsync(resolvedUri));
        }

        logger.LogInformation("{url} did not match any rules", @event.RawContent["url"]);

        return null;
    }
}
