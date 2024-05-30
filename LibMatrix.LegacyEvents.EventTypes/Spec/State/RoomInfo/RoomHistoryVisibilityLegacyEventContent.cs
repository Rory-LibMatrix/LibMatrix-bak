using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomHistoryVisibilityLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.history_visibility";

    [JsonPropertyName("history_visibility")]
    public string HistoryVisibility { get; set; }
}