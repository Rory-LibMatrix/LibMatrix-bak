using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
[MatrixEvent(EventName = "org.matrix.msc3765.topic", Legacy = true)]
public class RoomTopicEventContent : EventContent {
    public const string EventId = "m.room.topic";

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
}