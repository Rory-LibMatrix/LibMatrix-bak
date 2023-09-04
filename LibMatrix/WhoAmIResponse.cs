using System.Text.Json.Serialization;

namespace LibMatrix;

public class WhoAmIResponse {
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }
    [JsonPropertyName("is_guest")]
    public bool? IsGuest { get; set; }
}