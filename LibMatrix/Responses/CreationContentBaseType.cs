using System.Text.Json.Serialization;

namespace LibMatrix.Responses;

public class CreationContentBaseType {
    private readonly CreateRoomRequest _createRoomRequest;

    public CreationContentBaseType(CreateRoomRequest createRoomRequest) => this._createRoomRequest = createRoomRequest;

    [JsonPropertyName("type")]
    public string Type {
        get => (string)_createRoomRequest.CreationContent["type"];
        set {
            if (value is "null" or "") _createRoomRequest.CreationContent.Remove("type");
            else _createRoomRequest.CreationContent["type"] = value;
        }
    }
}
