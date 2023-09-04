using System.Collections.Generic;
using System.Text.Json.Serialization;
using LibMatrix.Responses;

namespace LibMatrix;

public class MessagesResponse {
    [JsonPropertyName("start")]
    public string Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("chunk")]
    public List<StateEventResponse> Chunk { get; set; } = new();

    [JsonPropertyName("state")]
    public List<StateEventResponse> State { get; set; } = new();
}
