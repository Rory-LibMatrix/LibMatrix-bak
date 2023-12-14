using System.Text.Json.Serialization;

namespace LibMatrix.Responses;

public class UserProfileResponse {
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; set; }
}
