using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.typing")]
public class RoomTypingEventData : IStateEventType {
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }
}
