using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomCanonicalAliasLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.canonical_alias";

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("alt_aliases")]
    public string[]? AltAliases { get; set; }
}