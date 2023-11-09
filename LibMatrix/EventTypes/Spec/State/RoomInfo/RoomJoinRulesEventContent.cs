using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.join_rules")]
public class RoomJoinRulesEventContent : TimelineEventContent {
    private static string Public = "public";
    private static string Invite = "invite";
    private static string Knock = "knock";

    /// <summary>
    /// one of ["public", "invite", "knock", "restricted", "knock_restricted"]
    /// "private" is reserved without implementation!
    /// </summary>
    [JsonPropertyName("join_rule")]
    public string JoinRule { get; set; }

    [JsonPropertyName("allow")]
    public List<AllowEntry> Allow { get; set; }

    public class AllowEntry {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("room_id")]
        public string RoomId { get; set; }
    }
}
