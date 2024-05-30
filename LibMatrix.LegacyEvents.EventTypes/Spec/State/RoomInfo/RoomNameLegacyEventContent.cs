using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomNameLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.name";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}