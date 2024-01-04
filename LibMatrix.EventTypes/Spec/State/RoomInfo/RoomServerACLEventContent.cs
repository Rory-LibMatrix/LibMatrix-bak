using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.server_acl")]
public class RoomServerACLEventContent : EventContent {
    [JsonPropertyName("allow")]
    public List<string>? Allow { get; set; } // = null!;

    [JsonPropertyName("deny")]
    public List<string>? Deny { get; set; } // = null!;

    [JsonPropertyName("allow_ip_literals")]
    public bool AllowIpLiterals { get; set; } // = false;
}