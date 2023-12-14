using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.space.parent")]
public class SpaceParentEventContent : TimelineEventContent {
    [JsonPropertyName("via")]
    public string[]? Via { get; set; }

    [JsonPropertyName("canonical")]
    public bool? Canonical { get; set; }
}
