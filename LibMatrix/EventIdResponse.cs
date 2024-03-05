using System.Text.Json.Serialization;

namespace LibMatrix;

public class EventIdResponse(string eventId) {
    public EventIdResponse(StateEventResponse stateEventResponse) : this(stateEventResponse.EventId ?? throw new NullReferenceException("State event ID is null!")) { }

    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = eventId;
}