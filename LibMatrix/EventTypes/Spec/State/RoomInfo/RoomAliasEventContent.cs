using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.alias")]
public class RoomAliasEventContent : EventContent {
    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}
