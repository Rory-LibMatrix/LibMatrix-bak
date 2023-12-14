using LibMatrix.EventTypes.Spec;
using LibMatrix.RoomTypes;

namespace LibMatrix.ExampleBot.Bot.Interfaces;

public class CommandContext {
    public GenericRoom Room { get; set; }
    public StateEventResponse MessageEvent { get; set; }
    public string CommandName => (MessageEvent.TypedContent as RoomMessageEventContent).Body.Split(' ')[0][1..];
    public string[] Args => (MessageEvent.TypedContent as RoomMessageEventContent).Body.Split(' ')[1..];
}
