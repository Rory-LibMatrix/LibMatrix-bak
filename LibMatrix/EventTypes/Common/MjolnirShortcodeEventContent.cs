using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Common;

[MatrixEvent(EventName = "org.matrix.mjolnir.shortcode")]
public class MjolnirShortcodeEventContent : EventContent {
    [JsonPropertyName("shortcode")]
    public string? Shortcode { get; set; }
}
