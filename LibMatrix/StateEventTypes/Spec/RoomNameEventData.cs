using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.name")]
public class RoomNameEventContent : EventContent {
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
