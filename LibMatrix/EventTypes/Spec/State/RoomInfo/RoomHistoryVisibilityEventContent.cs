using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.history_visibility")]
public class RoomHistoryVisibilityEventContent : TimelineEventContent {
    [JsonPropertyName("history_visibility")]
    public string HistoryVisibility { get; set; }
}
