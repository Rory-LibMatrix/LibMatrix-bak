using System.Text.Json.Serialization;

namespace LibMatrix.Responses.ModAS;

public class ModASRoomQueryResult {
    [JsonPropertyName("room_id")]
    public required string RoomId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("canonical_alias")]
    public string? CanonicalAlias { get; set; }

    [JsonPropertyName("joined_members")]
    public int JoinedMembers { get; set; }

    [JsonPropertyName("joined_local_members")]
    public int JoinedLocalMembers { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("encryption")]
    public string? Encryption { get; set; }

    [JsonPropertyName("federatable")]
    public bool Federatable { get; set; }

    [JsonPropertyName("public")]
    public bool Public { get; set; }

    [JsonPropertyName("join_rules")]
    public string? JoinRules { get; set; }

    [JsonPropertyName("guest_access")]
    public string? GuestAccess { get; set; }

    [JsonPropertyName("history_visibility")]
    public string? HistoryVisibility { get; set; }

    [JsonPropertyName("state_events")]
    public int StateEvents { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("topic")]
    public string? RoomTopic { get; set; }

    [JsonPropertyName("total_members")]
    public int TotalMembers { get; set; }
    
    [JsonPropertyName("total_local_members")]
    public int TotalLocalMembers { get; set; }
}