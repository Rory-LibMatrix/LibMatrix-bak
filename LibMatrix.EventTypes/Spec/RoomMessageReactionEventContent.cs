namespace LibMatrix.EventTypes.Spec;

[MatrixEvent(EventName = EventId)]
public class RoomMessageReactionEventContent : TimelineEventContent {
    public const string EventId = "m.reaction";
}