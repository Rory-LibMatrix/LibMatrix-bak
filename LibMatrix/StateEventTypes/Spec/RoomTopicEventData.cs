using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.topic")]
[MatrixEvent(EventName = "org.matrix.msc3765.topic", Legacy = true)]
public class RoomTopicEventData : IStateEventType {
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
}
