using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.name")]
public class RoomNameEventContent : EventContent {
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
