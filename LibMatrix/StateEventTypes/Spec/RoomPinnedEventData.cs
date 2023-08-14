using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.pinned_events")]
public class RoomPinnedEventData : IStateEventType {
    [JsonPropertyName("pinned")]
    public string[]? PinnedEvents { get; set; }
}
