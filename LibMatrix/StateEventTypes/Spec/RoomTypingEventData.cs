using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.typing")]
public class RoomTypingEventContent : EventContent {
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }
}
