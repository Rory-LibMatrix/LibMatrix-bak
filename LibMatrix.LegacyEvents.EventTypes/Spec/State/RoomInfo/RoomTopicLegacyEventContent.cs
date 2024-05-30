using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
[LegacyMatrixEvent(EventName = "org.matrix.msc3765.topic", Legacy = true)]
public class RoomTopicLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.topic";

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
}