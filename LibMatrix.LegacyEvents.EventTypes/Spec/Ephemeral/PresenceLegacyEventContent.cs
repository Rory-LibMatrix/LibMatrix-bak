using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.Ephemeral;

[LegacyMatrixEvent(EventName = EventId)]
public class PresenceLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.presence";

    [JsonPropertyName("presence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Presence { get; set; }

    [JsonPropertyName("last_active_ago")]
    public long LastActiveAgo { get; set; }

    [JsonPropertyName("currently_active")]
    public bool CurrentlyActive { get; set; }

    [JsonPropertyName("status_msg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusMessage { get; set; }

    [JsonPropertyName("avatar_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("displayname")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}