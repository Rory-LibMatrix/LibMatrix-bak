using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.space.child")]
public class SpaceChildEventContent : EventContent {
    [JsonPropertyName("auto_join")]
    public bool? AutoJoin { get; set; }
    [JsonPropertyName("via")]
    public List<string>? Via { get; set; }
    [JsonPropertyName("suggested")]
    public bool? Suggested { get; set; }
}
