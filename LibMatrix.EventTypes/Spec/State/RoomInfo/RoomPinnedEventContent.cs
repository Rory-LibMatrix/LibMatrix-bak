using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomPinnedEventContent : EventContent {
    public const string EventId = "m.room.pinned_events";

    [JsonPropertyName("pinned")]
    public string[]? PinnedEvents { get; set; }
}