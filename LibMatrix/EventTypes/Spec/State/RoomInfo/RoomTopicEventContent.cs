using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.topic")]
[MatrixEvent(EventName = "org.matrix.msc3765.topic", Legacy = true)]
public class RoomTopicEventContent : EventContent {
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
}
