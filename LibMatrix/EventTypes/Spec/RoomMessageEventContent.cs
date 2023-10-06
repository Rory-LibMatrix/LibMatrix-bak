using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec;

[MatrixEvent(EventName = "m.room.message")]
public class RoomMessageEventContent : EventContent {
    public RoomMessageEventContent(string? messageType = "m.notice", string? body = null) {
        MessageType = messageType;
        Body = body;
    }

    [JsonPropertyName("body")]
    public string Body { get; set; }

    [JsonPropertyName("msgtype")]
    public string MessageType { get; set; } = "m.notice";

    [JsonPropertyName("formatted_body")]
    public string FormattedBody { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    /// <summary>
    /// Media URI for this message, if any
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public string? FileName { get; set; }
}
