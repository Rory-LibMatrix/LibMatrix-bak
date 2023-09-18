using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.pinned_events")]
public class RoomPinnedEventContent : EventContent {
    [JsonPropertyName("pinned")]
    public string[]? PinnedEvents { get; set; }
}
