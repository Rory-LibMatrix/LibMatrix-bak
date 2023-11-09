using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.server_acl")]
public class RoomServerACLEventContent : TimelineEventContent {
    [JsonPropertyName("allow")]
    public List<string> Allow { get; set; } // = null!;

    [JsonPropertyName("deny")]
    public List<string> Deny { get; set; } // = null!;

    [JsonPropertyName("allow_ip_literals")]
    public bool AllowIpLiterals { get; set; } // = false;
}
