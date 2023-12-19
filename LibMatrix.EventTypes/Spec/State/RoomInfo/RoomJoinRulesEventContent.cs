using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.join_rules")]
public class RoomJoinRulesEventContent : EventContent {
    /// <summary>
    /// one of ["public", "invite", "knock", "restricted", "knock_restricted"]
    /// "private" is reserved without implementation!
    /// </summary>
    [JsonPropertyName("join_rule")]
    public string JoinRuleValue { get; set; }
    
    [JsonIgnore]
    public JoinRules JoinRule {
        get => JoinRuleValue switch {
            "public" => JoinRules.Public,
            "invite" => JoinRules.Invite,
            "knock" => JoinRules.Knock,
            "restricted" => JoinRules.Restricted,
            "knock_restricted" => JoinRules.KnockRestricted,
            _ => throw new ArgumentOutOfRangeException()
        };
        set => JoinRuleValue = value switch {
            JoinRules.Public => "public",
            JoinRules.Invite => "invite",
            JoinRules.Knock => "knock",
            JoinRules.Restricted => "restricted",
            JoinRules.KnockRestricted => "knock_restricted",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    [JsonPropertyName("allow")]
    public List<AllowEntry>? Allow { get; set; }

    public class AllowEntry {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("room_id")]
        public required string RoomId { get; set; }
    }

    public enum JoinRules {
        Public,
        Invite,
        Knock,
        Restricted,
        KnockRestricted
    }
}
