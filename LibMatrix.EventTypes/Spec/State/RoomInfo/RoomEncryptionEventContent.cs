using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.encryption")]
public class RoomEncryptionEventContent : TimelineEventContent {
    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }
    [JsonPropertyName("rotation_period_ms")]
    public ulong? RotationPeriodMs { get; set; }
    [JsonPropertyName("rotation_period_msgs")]
    public ulong? RotationPeriodMsgs { get; set; }
}
