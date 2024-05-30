using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomTypingLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.typing";

    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }
}