using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.canonical_alias")]
public class CanonicalAliasEventContent : EventContent {
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
    [JsonPropertyName("alt_aliases")]
    public string[]? AltAliases { get; set; }
}
