using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.pinned_events")]
public class RoomPinnedEventContent : TimelineEventContent {
    [JsonPropertyName("pinned")]
    public string[]? PinnedEvents { get; set; }
}
