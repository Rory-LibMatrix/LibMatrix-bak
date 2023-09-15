using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.space.parent")]
public class SpaceParentEventContent : EventContent {
    [JsonPropertyName("via")]
    public string[]? Via { get; set; }

    [JsonPropertyName("canonical")]
    public bool? Canonical { get; set; }
}
