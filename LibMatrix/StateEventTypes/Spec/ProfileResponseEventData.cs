using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

public class ProfileResponseEventData : IStateEventType {
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; } = "";

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; set; } = "";
}
