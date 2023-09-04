using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.member")]
public class RoomMemberEventData : IStateEventType {
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("membership")]
    public string Membership { get; set; } = null!;

    [JsonPropertyName("displayname")]
    public string? Displayname { get; set; }

    [JsonPropertyName("is_direct")]
    public bool? IsDirect { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("join_authorised_via_users_server")]
    public string? JoinAuthorisedViaUsersServer { get; set; }
}
