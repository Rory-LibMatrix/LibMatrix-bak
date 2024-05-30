namespace LibMatrix.LegacyEvents.EventTypes.Spec;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomMessageReactionLegacyEventContent : TimelineLegacyEventContent {
    public const string EventId = "m.reaction";
}