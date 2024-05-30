using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomPinnedLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.pinned_events";

    [JsonPropertyName("pinned")]
    public string[]? PinnedEvents { get; set; }
}