using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LibMatrix.Responses;

public class RoomKeysRequest {
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; }
    
    [JsonPropertyName("auth_data")]
    public JsonObject AuthData { get; set; }
}
public class RoomKeysResponse : RoomKeysRequest {
    [JsonPropertyName("version")]
    public string Version { get; set; }
    
    [JsonPropertyName("etag")]
    public string Etag { get; set; }
}