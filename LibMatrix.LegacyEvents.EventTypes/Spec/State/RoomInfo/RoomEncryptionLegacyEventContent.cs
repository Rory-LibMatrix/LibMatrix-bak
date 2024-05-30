using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomEncryptionLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.encryption";

    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }

    [JsonPropertyName("rotation_period_ms")]
    public ulong? RotationPeriodMs { get; set; }

    [JsonPropertyName("rotation_period_msgs")]
    public ulong? RotationPeriodMsgs { get; set; }
}