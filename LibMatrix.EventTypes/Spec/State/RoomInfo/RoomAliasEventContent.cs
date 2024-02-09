using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State.RoomInfo;

[MatrixEvent(EventName = EventId)]
public class RoomAliasEventContent : EventContent {
    public const string EventId = "m.room.alias";

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}