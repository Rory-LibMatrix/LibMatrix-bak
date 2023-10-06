using System.Text.Json.Serialization;
using LibMatrix;
using LibMatrix.RoomTypes;
using MediaModeratorPoC.StateEventTypes;

namespace MediaModeratorPoC;

public class PolicyList {
    [JsonIgnore]
    public GenericRoom Room { get; set; }

    [JsonPropertyName("trusted")]
    public bool Trusted { get; set; } = false;

    [JsonIgnore]
    public List<StateEvent> Policies { get; set; } = new();
}
