using System.Text.Json.Serialization;

namespace LibMatrix.Responses;

public class ClientVersionsResponse {
    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();

    [JsonPropertyName("unstable_features")]
    public Dictionary<string, bool> UnstableFeatures { get; set; } = new();
}
