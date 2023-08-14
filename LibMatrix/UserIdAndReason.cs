using System.Text.Json.Serialization;

namespace LibMatrix;

internal class UserIdAndReason {
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
