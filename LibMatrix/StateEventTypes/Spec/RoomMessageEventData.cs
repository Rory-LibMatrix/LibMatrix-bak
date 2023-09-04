using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.message")]
public class RoomMessageEventData : IStateEventType {
    [JsonPropertyName("body")]
    public string Body { get; set; }
    [JsonPropertyName("msgtype")]
    public string MessageType { get; set; } = "m.notice";

    [JsonPropertyName("formatted_body")]
    public string FormattedBody { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }
}
