using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class SpaceParentEventContent : EventContent {
    public const string EventId = "m.space.parent";

    [JsonPropertyName("via")]
    public string[]? Via { get; set; }

    [JsonPropertyName("canonical")]
    public bool? Canonical { get; set; }
}