using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.canonical_alias")]
public class CanonicalAliasEventContent : EventContent {
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
    [JsonPropertyName("alt_aliases")]
    public string[]? AltAliases { get; set; }
}
