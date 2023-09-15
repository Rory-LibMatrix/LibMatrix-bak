using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.alias")]
public class RoomAliasEventContent : EventContent {
    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}
