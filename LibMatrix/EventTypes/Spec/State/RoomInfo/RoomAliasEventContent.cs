using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomAliasEventContent : TimelineEventContent {
    public const string EventId = "m.room.alias";

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}
