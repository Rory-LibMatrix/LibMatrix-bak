using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.history_visibility")]
public class HistoryVisibilityEventContent : EventContent {
    [JsonPropertyName("history_visibility")]
    public string HistoryVisibility { get; set; }
}
