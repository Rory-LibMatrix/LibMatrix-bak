using System.Text.Json.Serialization;

namespace LibMatrix;

public class MessagesResponse {
    [JsonPropertyName("start")]
    public string Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("chunk")]
    public List<LegacyMatrixEventResponse> Chunk { get; set; } = new();

    [JsonPropertyName("state")]
    public List<LegacyMatrixEventResponse> State { get; set; } = new();
}