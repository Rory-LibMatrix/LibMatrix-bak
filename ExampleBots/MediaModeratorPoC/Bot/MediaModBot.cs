using System.Buffers.Text;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using ArcaneLibs.Extensions;
using LibMatrix;
using LibMatrix.Helpers;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using LibMatrix.Services;
using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.AccountData;
using MediaModeratorPoC.Bot.Interfaces;
using MediaModeratorPoC.Bot.StateEventTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaModeratorPoC.Bot;

public class MediaModBot : IHostedService {
    private readonly AuthenticatedHomeserverGeneric _hs;
    private readonly ILogger<MediaModBot> _logger;
    private readonly MediaModBotConfiguration _configuration;
    private readonly HomeserverResolverService _hsResolver;
    private readonly IEnumerable<ICommand> _commands;

    private Task _listenerTask;

    private GenericRoom _policyRoom;
    private GenericRoom _logRoom;
    private GenericRoom _controlRoom;

    public MediaModBot(AuthenticatedHomeserverGeneric hs, ILogger<MediaModBot> logger,
        MediaModBotConfiguration configuration, HomeserverResolverService hsResolver) {
        logger.LogInformation("{} instantiated!", this.GetType().Name);
        _hs = hs;
        _logger = logger;
        _configuration = configuration;
        _hsResolver = hsResolver;
    }

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken) {
        _listenerTask = Run(cancellationToken);
        _logger.LogInformation("Bot started!");
    }

    private async Task Run(CancellationToken cancellationToken) {
        Directory.GetFiles("bot_data/cache").ToList().ForEach(File.Delete);

        BotData botData;

        try {
            botData = await _hs.GetAccountData<BotData>("gay.rory.media_moderator_poc_data");
        }
        catch (Exception e) {
            if (e is not MatrixException { ErrorCode: "M_NOT_FOUND" }) {
                _logger.LogError("{}", e.ToString());
                throw;
            }

            botData = new BotData();
            var creationContent = CreateRoomRequest.CreatePrivate(_hs, name: "Media Moderator PoC - Control room", roomAliasName: "media-moderator-poc-control-room");
            creationContent.Invite = _configuration.Admins;
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.control_room";

            botData.ControlRoom = (await _hs.CreateRoom(creationContent)).RoomId;

            //set access rules to allow joining via control room
            creationContent.InitialState.Add(new StateEvent {
                Type = "m.room.join_rules",
                StateKey = "",
                TypedContent = new JoinRulesEventData {
                    JoinRule = "knock_restricted",
                    Allow = new() {
                        new JoinRulesEventData.AllowEntry {
                            Type = "m.room_membership",
                            RoomId = botData.ControlRoom
                        }
                    }
                }
            });

            creationContent.Name = "Media Moderator PoC - Log room";
            creationContent.RoomAliasName = "media-moderator-poc-log-room";
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.log_room";
            botData.LogRoom = (await _hs.CreateRoom(creationContent)).RoomId;

            creationContent.Name = "Media Moderator PoC - Policy room";
            creationContent.RoomAliasName = "media-moderator-poc-policy-room";
            creationContent.CreationContent["type"] = "gay.rory.media_moderator_poc.policy_room";
            botData.PolicyRoom = (await _hs.CreateRoom(creationContent)).RoomId;

            await _hs.SetAccountData("gay.rory.media_moderator_poc_data", botData);
        }

        _policyRoom = await _hs.GetRoom(botData.PolicyRoom ?? botData.ControlRoom);
        _logRoom = await _hs.GetRoom(botData.LogRoom ?? botData.ControlRoom);
        _controlRoom = await _hs.GetRoom(botData.ControlRoom);

        List<string> admins = new();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () => {
            while (!cancellationToken.IsCancellationRequested) {
                var controlRoomMembers = _controlRoom.GetMembersAsync();
                await foreach (var member in controlRoomMembers) {
                    if ((member.TypedContent as RoomMemberEventData).Membership == "join") admins.Add(member.UserId);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }, cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        _hs.SyncHelper.InviteReceivedHandlers.Add(async Task (args) => {
            var inviteEvent =
                args.Value.InviteState.Events.FirstOrDefault(x =>
                    x.Type == "m.room.member" && x.StateKey == _hs.WhoAmI.UserId);
            _logger.LogInformation(
                $"Got invite to {args.Key} by {inviteEvent.Sender} with reason: {(inviteEvent.TypedContent as RoomMemberEventData).Reason}");
            if (inviteEvent.Sender.EndsWith(":rory.gay") || inviteEvent.Sender.EndsWith(":conduit.rory.gay")) {
                try {
                    var senderProfile = await _hs.GetProfile(inviteEvent.Sender);
                    await (await _hs.GetRoom(args.Key)).JoinAsync(reason: $"I was invited by {senderProfile.DisplayName ?? inviteEvent.Sender}!");
                }
                catch (Exception e) {
                    _logger.LogError("{}", e.ToString());
                    await (await _hs.GetRoom(args.Key)).LeaveAsync(reason: "I was unable to join the room: " + e);
                }
            }
        });

        _hs.SyncHelper.TimelineEventHandlers.Add(async @event => {
            var room = await _hs.GetRoom(@event.RoomId);
            try {
                _logger.LogInformation(
                    "Got timeline event in {}: {}", @event.RoomId, @event.ToJson(indent: true, ignoreNull: true));

                if (@event is { Type: "m.room.message", TypedContent: RoomMessageEventData message }) {
                    if (message is { MessageType: "m.image" }) {
                        //check media
                        var matchedPolicy = await CheckMedia(@event);
                        if (matchedPolicy is null) return;
                        var matchedpolicyData = matchedPolicy.TypedContent as MediaPolicyStateEventData;
                        var recommendation = matchedpolicyData.Recommendation;
                        await _logRoom.SendMessageEventAsync("m.room.message",
                            new RoomMessageEventData(
                                body:
                                $"User {MessageFormatter.HtmlFormatMention(@event.Sender)} posted an image in {MessageFormatter.HtmlFormatMention(room.RoomId)} that matched rule {matchedPolicy.StateKey}, applying action {matchedpolicyData.Recommendation}, as described in rule: {matchedPolicy.RawContent!.ToJson(ignoreNull: true)}",
                                messageType: "m.text") {
                                Format = "org.matrix.custom.html",
                                FormattedBody =
                                    $"<font color=\"#FFFF00\">User {MessageFormatter.HtmlFormatMention(@event.Sender)} posted an image in {MessageFormatter.HtmlFormatMention(room.RoomId)} that matched rule {matchedPolicy.StateKey}, applying action {matchedpolicyData.Recommendation}, as described in rule: <pre>{matchedPolicy.RawContent!.ToJson(ignoreNull: true)}</pre></font>"
                            });
                        switch (recommendation) {
                            case "warn_admins": {
                                await _controlRoom.SendMessageEventAsync("m.room.message",
                                    new RoomMessageEventData(body: $"{string.Join(' ', admins)}\nUser {MessageFormatter.HtmlFormatMention(@event.Sender)} posted a banned image {message.Url}",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody = $"{string.Join(' ', admins.Select(u=>MessageFormatter.HtmlFormatMention(u)))}\n" +
                                                        $"<font color=\"#FF0000\">User {MessageFormatter.HtmlFormatMention(@event.Sender)} posted a banned image <a href=\"{message.Url}\">{message.Url}</a></font>"
                                    });
                                break;
                            }
                            case "warn": {
                                await room.SendMessageEventAsync("m.room.message",
                                    new RoomMessageEventData(
                                        body: $"Please be careful when posting this image: {matchedpolicyData.Reason}",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody =
                                            $"<font color=\"#FFFF00\">Please be careful when posting this image: {matchedpolicyData.Reason}</a></font>"
                                    });
                                break;
                            }
                            case "redact": {
                                await room.RedactEventAsync(@event.EventId, matchedpolicyData.Reason);
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
                                await room.SendMessageEventAsync("m.room.message",
                                    new RoomMessageEventData(
                                        body:
                                        $"Please be careful when posting this image: {matchedpolicyData.Reason}, I have spoilered it for you:",
                                        messageType: "m.text") {
                                        Format = "org.matrix.custom.html",
                                        FormattedBody =
                                            $"<font color=\"#FFFF00\">Please be careful when posting this image: {matchedpolicyData.Reason}, I have spoilered it for you:</a></font>"
                                    });
                                var imageUrl = message.Url;
                                await room.SendMessageEventAsync("m.room.message",
                                    new RoomMessageEventData(body: $"CN: {imageUrl}",
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
                _logger.LogError("{}", e.ToString());
                await _controlRoom.SendMessageEventAsync("m.room.message",
                    MessageFormatter.FormatException($"Unable to ban user in {MessageFormatter.HtmlFormatMention(room.RoomId)}", e));
                await _logRoom.SendMessageEventAsync("m.room.message",
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
        _logger.LogInformation("Shutting down bot!");
    }

    private async Task<StateEventResponse?> CheckMedia(StateEventResponse @event) {
        var stateList = _policyRoom.GetFullStateAsync();
        var hashAlgo = SHA3_256.Create();

        var mxcUri = @event.RawContent["url"].GetValue<string>();
        var resolvedUri = await _hsResolver.ResolveMediaUri(mxcUri.Split('/')[2], mxcUri);
        var uriHash = hashAlgo.ComputeHash(mxcUri.AsBytes().ToArray());
        byte[]? fileHash = null;

        try {
            fileHash = await hashAlgo.ComputeHashAsync(await _hs._httpClient.GetStreamAsync(resolvedUri));
        }
        catch (Exception ex) {
            await _logRoom.SendMessageEventAsync("m.room.message",
                MessageFormatter.FormatException($"Error calculating file hash for {mxcUri} via {mxcUri.Split('/')[2]} ({resolvedUri}), retrying via {_hs.HomeServerDomain}...",
                    ex));
            try {
                resolvedUri = await _hsResolver.ResolveMediaUri(_hs.HomeServerDomain, mxcUri);
                fileHash = await hashAlgo.ComputeHashAsync(await _hs._httpClient.GetStreamAsync(resolvedUri));
            }
            catch (Exception ex2) {
                await _logRoom.SendMessageEventAsync("m.room.message",
                    MessageFormatter.FormatException($"Error calculating file hash via {_hs.HomeServerDomain} ({resolvedUri})!", ex2));
            }
        }

        _logger.LogInformation("Checking media {url} with hash {hash}", resolvedUri, fileHash);

        await foreach (var state in stateList) {
            if (state.Type != "gay.rory.media_moderator_poc.rule.media" && state.Type != "gay.rory.media_moderator_poc.rule.server") continue;
            if (!state.RawContent.ContainsKey("entity")) {
                _logger.LogWarning("Rule {rule} has no entity, this event was probably redacted!", state.StateKey);
                continue;
            }
            _logger.LogInformation("Checking rule {rule}: {data}", state.StateKey, state.TypedContent.ToJson(ignoreNull: true, indent: false));
            var rule = state.TypedContent as MediaPolicyStateEventData;
            if (state.Type == "gay.rory.media_moderator_poc.rule.server" && rule.ServerEntity is not null) {
                rule.ServerEntity = rule.ServerEntity.Replace("\\*", ".*").Replace("\\?", ".");
                var regex = new Regex($"mxc://({rule.ServerEntity})/.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (regex.IsMatch(@event.RawContent["url"].GetValue<string>())) {
                    _logger.LogInformation("{url} matched rule {rule}", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                    return state;
                }
            }

            if (rule.Entity is not null && uriHash.SequenceEqual(rule.Entity)) {
                _logger.LogInformation("{url} matched rule {rule} by uri hash", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                return state;
            }

            _logger.LogInformation("uri hash {uriHash} did not match rule's {ruleUriHash}",  Convert.ToBase64String(uriHash), Convert.ToBase64String(rule.Entity));

            if (rule.FileHash is not null && fileHash is not null && rule.FileHash.SequenceEqual(fileHash)) {
                _logger.LogInformation("{url} matched rule {rule} by file hash", @event.RawContent["url"], rule.ToJson(ignoreNull: true));
                return state;
            }

            _logger.LogInformation("file hash {fileHash} did not match rule's {ruleFileHash}", Convert.ToBase64String(fileHash), Convert.ToBase64String(rule.FileHash));


            //check pixels every 10% of the way through the image using ImageSharp
            // var image = Image.Load(await _hs._httpClient.GetStreamAsync(resolvedUri));
        }


        _logger.LogInformation("{url} did not match any rules", @event.RawContent["url"]);

        return null;
    }
}
