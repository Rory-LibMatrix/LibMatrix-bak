using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Common;

[MatrixEvent(EventName = "org.matrix.mjolnir.shortcode")]
public class MjolnirShortcodeEventContent : TimelineEventContent {
    [JsonPropertyName("shortcode")]
    public string? Shortcode { get; set; }
}
