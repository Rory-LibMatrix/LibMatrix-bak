using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.history_visibility")]
public class RoomHistoryVisibilityEventContent : TimelineEventContent {
    [JsonPropertyName("history_visibility")]
    public required string HistoryVisibility { get; set; }
}
