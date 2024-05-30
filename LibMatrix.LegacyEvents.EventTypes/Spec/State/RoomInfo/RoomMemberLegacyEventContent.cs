using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomMemberLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.member";

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("membership")]
    public required string Membership { get; set; }

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("is_direct")]
    public bool? IsDirect { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("join_authorised_via_users_server")]
    public string? JoinAuthorisedViaUsersServer { get; set; }
    
    public static class MembershipTypes {
        public const string Invite = "invite";
        public const string Join = "join";
        public const string Leave = "leave";
        public const string Ban = "ban";
        public const string Knock = "knock";
    }
}