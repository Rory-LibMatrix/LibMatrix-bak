using LibMatrix.EventTypes.Spec;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;

namespace LibMatrix.Utilities.Bot.Interfaces;

public class CommandContext {
    public GenericRoom Room { get; set; }
    public StateEventResponse MessageEvent { get; set; }

    public string MessageContentWithoutReply =>
        (MessageEvent.TypedContent as RoomMessageEventContent)!
        .Body.Split('\n')
        .SkipWhile(x => x.StartsWith(">"))
        .Aggregate((x, y) => $"{x}\n{y}");

    public string CommandName => MessageContentWithoutReply.Split(' ')[0][1..];
    public string[] Args => MessageContentWithoutReply.Split(' ')[1..];
    public AuthenticatedHomeserverGeneric Homeserver { get; set; }

    public async Task<EventIdResponse> Reply(RoomMessageEventContent content) => await Room.SendMessageEventAsync(content);
}
