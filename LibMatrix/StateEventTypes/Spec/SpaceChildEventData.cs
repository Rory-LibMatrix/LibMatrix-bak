using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.space.child")]
public class SpaceChildEventContent : EventContent {
    [JsonPropertyName("auto_join")]
    public bool? AutoJoin { get; set; }
    [JsonPropertyName("via")]
    public string[]? Via { get; set; }
    [JsonPropertyName("suggested")]
    public bool? Suggested { get; set; }
}
