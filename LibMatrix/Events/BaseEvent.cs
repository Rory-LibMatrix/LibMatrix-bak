using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LibMatrix.Events;

public class StateEvent {
    [JsonPropertyName("state_key")]
    public string? StateKey { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("replaces_state")]
    public string? ReplacesState { get; set; }
}

public class StateEventResponse : StateEvent {
    [JsonPropertyName("origin_server_ts")]
    public long? OriginServerTs { get; set; }

    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("unsigned")]
    public UnsignedData? Unsigned { get; set; }

    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    public class UnsignedData {
        [JsonPropertyName("age")]
        public ulong? Age { get; set; }

        [JsonPropertyName("redacted_because")]
        public object? RedactedBecause { get; set; }

        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("replaces_state")]
        public string? ReplacesState { get; set; }

        [JsonPropertyName("prev_sender")]
        public string? PrevSender { get; set; }

        [JsonPropertyName("prev_content")]
        public JsonObject? PrevContent { get; set; }
    }
}