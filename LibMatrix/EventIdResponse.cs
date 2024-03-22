using System.Text.Json.Serialization;

namespace LibMatrix;

public class EventIdResponse {
    [JsonPropertyName("event_id")]
    public string EventId { get; set; }
}