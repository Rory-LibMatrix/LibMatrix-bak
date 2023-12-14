using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomNameEventContent : TimelineEventContent {
    public const string EventId = "m.room.name";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
