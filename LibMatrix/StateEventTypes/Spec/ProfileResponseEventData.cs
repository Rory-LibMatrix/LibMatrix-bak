using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

public class ProfileResponseEventContent : EventContent {
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; set; }
}
