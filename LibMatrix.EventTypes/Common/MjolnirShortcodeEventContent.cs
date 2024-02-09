using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Common;

[MatrixEvent(EventName = EventId)]
public class MjolnirShortcodeEventContent : TimelineEventContent {
    public const string EventId = "org.matrix.mjolnir.shortcode";

    [JsonPropertyName("shortcode")]
    public string? Shortcode { get; set; }
}
