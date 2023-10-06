using LibMatrix.EventTypes.Spec;
using LibMatrix.Helpers;
using LibMatrix.Homeservers;
using LibMatrix.RoomTypes;
using LibMatrix.Services;
using MediaModeratorPoC.AccountData;
using MediaModeratorPoC.StateEventTypes;
using Microsoft.Extensions.Logging;

namespace MediaModeratorPoC;

public class PolicyEngine(AuthenticatedHomeserverGeneric hs, ILogger<MediaModBot> logger, MediaModBotConfiguration configuration,
    HomeserverResolverService hsResolver) {
    public List<PolicyList> ActivePolicyLists { get; set; } = new();
    private GenericRoom? _logRoom;
    private GenericRoom? _controlRoom;

    public async Task ReloadActivePolicyLists() {
        // first time init
        if (_logRoom is null || _controlRoom is null) {
            var botData = await hs.GetAccountDataAsync<BotData>("gay.rory.modbot_data");
            _logRoom ??= hs.GetRoom(botData.LogRoom ?? botData.ControlRoom);
            _controlRoom ??= hs.GetRoom(botData.ControlRoom);
        }

        await _controlRoom?.SendMessageEventAsync(MessageFormatter.FormatSuccess("Reloading policy lists!"))!;
        await _logRoom?.SendMessageEventAsync(
            new RoomMessageEventContent(
                body: "Reloading policy lists!",
                messageType: "m.text"))!;

        await _controlRoom?.SendMessageEventAsync(MessageFormatter.FormatSuccess("0/? policy lists loaded"))!;

        var policyLists = new List<PolicyList>();
        var policyListAccountData = await hs.GetAccountDataAsync<Dictionary<string, PolicyList>>("gay.rory.modbot.policy_lists");
        foreach (var (roomId, policyList) in policyListAccountData) {
            _logRoom?.SendMessageEventAsync(
                new RoomMessageEventContent(
                    body: $"Loading policy list {MessageFormatter.HtmlFormatMention(roomId)}!",
                    messageType: "m.text"));
            var room = hs.GetRoom(roomId);

            policyList.Room = room;

            var stateEvents = room.GetFullStateAsync();
            await foreach (var stateEvent in stateEvents) {
                if (stateEvent != null && stateEvent.GetType.IsAssignableTo(typeof(BasePolicy))) {
                    policyList.Policies.Add(stateEvent);
                }
            }

            //html table of policy count by type
            var policyCount = policyList.Policies.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
            var policyCountTable = policyCount.Aggregate(
                "<table><tr><th>Policy Type</th><th>Count</th></tr>",
                (current, policy) => current + $"<tr><td>{policy.Key}</td><td>{policy.Value}</td></tr>");
            policyCountTable += "</table>";

            var policyCountTablePlainText = policyCount.Aggregate(
                "Policy Type       | Count\n",
                (current, policy) => current + $"{policy.Key,-16} | {policy.Value}\n");
            await _logRoom?.SendMessageEventAsync(
                new RoomMessageEventContent() {
                    MessageType = "org.matrix.custom.html",
                    Body = $"Policy count for {roomId}:\n{policyCountTablePlainText}",
                    FormattedBody = $"Policy count for {MessageFormatter.HtmlFormatMention(roomId)}:\n{policyCountTable}",
                })!;

            await _logRoom?.SendMessageEventAsync(
                new RoomMessageEventContent(
                    body: $"Loaded {policyList.Policies.Count} policies for {MessageFormatter.HtmlFormatMention(roomId)}!",
                    messageType: "m.text"))!;

            policyLists.Add(policyList);

            var progressMsgContent = MessageFormatter.FormatSuccess($"{policyLists.Count}/{policyListAccountData.Count} policy lists loaded");
            //edit old message
            progressMsgContent.RelatesTo = new() {

            };
            _controlRoom?.SendMessageEventAsync(progressMsgContent);
        }

        ActivePolicyLists = policyLists;
    }
}
