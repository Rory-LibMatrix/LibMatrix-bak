using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomTypingEventContent : TimelineEventContent {
    public const string EventId = "m.typing";

    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }
}
