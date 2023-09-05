using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Common;

[MatrixEvent(EventName = "org.matrix.mjolnir.shortcode")]
public class MjolnirShortcodeEventData : IStateEventType {
    [JsonPropertyName("shortcode")]
    public string? Shortcode { get; set; }
}
