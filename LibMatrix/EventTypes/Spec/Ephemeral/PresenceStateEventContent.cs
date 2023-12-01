using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class PresenceEventContent : EventContent {
    public const string EventId = "m.presence";

    [JsonPropertyName("presence"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Presence { get; set; }
    [JsonPropertyName("last_active_ago")]
    public long LastActiveAgo { get; set; }
    [JsonPropertyName("currently_active")]
    public bool CurrentlyActive { get; set; }
    [JsonPropertyName("status_msg"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusMessage { get; set; }
    [JsonPropertyName("avatar_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; set; }
    [JsonPropertyName("displayname"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}
