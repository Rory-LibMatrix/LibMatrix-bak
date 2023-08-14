using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using LibMatrix.StateEventTypes.Spec;

namespace LibMatrix.ExampleBot.Bot.Interfaces;

public class CommandContext {
    public GenericRoom Room { get; set; }
    public StateEventResponse MessageEvent { get; set; }
    public string CommandName => (MessageEvent.TypedContent as RoomMessageEventData).Body.Split(' ')[0][1..];
    public string[] Args => (MessageEvent.TypedContent as RoomMessageEventData).Body.Split(' ')[1..];
}
