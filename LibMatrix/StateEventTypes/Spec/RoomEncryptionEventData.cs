using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.encryption")]
public class RoomEncryptionEventContent : EventContent {
    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }
    [JsonPropertyName("rotation_period_ms")]
    public ulong? RotationPeriodMs { get; set; }
    [JsonPropertyName("rotation_period_msgs")]
    public ulong? RotationPeriodMsgs { get; set; }
}
