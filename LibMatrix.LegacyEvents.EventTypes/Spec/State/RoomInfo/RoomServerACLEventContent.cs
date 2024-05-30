using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomServerACLEventContent : EventContent {
    public const string EventId = "m.room.server_acl";

    [JsonPropertyName("allow")]
    public List<string>? Allow { get; set; } // = null!;

    [JsonPropertyName("deny")]
    public List<string>? Deny { get; set; } // = null!;

    [JsonPropertyName("allow_ip_literals")]
    public bool AllowIpLiterals { get; set; } // = false;
}