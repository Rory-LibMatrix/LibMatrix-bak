using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class SpaceChildLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.space.child";

    [JsonPropertyName("auto_join")]
    public bool? AutoJoin { get; set; }

    [JsonPropertyName("via")]
    public List<string>? Via { get; set; }

    [JsonPropertyName("suggested")]
    public bool? Suggested { get; set; }
}