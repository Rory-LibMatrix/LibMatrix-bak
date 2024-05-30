using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Common;

[LegacyMatrixEvent(EventName = EventId)]
public class MjolnirShortcodeLegacyEventContent : TimelineLegacyEventContent {
    public const string EventId = "org.matrix.mjolnir.shortcode";

    [JsonPropertyName("shortcode")]
    public string? Shortcode { get; set; }
}