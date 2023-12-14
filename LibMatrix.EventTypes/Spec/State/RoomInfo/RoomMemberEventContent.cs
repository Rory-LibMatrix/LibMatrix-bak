using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomMemberEventContent : TimelineEventContent {
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
}
