using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State.RoomInfo;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomAliasLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.alias";

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}