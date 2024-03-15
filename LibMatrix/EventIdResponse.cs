using System.Text.Json.Serialization;

namespace LibMatrix;

public class EventIdResponse(string eventId) {
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = eventId;
}