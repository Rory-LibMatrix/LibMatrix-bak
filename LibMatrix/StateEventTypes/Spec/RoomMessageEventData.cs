using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.message")]
public class RoomMessageEventData : IStateEventType {
    public RoomMessageEventData() { }

    public RoomMessageEventData(string messageType, string body) {
        MessageType = messageType;
        Body = body;
    }

    public RoomMessageEventData(string body) : this() {
        Body = body;
        MessageType = "m.notice";
    }

    [JsonPropertyName("body")]
    public string Body { get; set; }

    [JsonPropertyName("msgtype")]
    public string MessageType { get; set; } = "m.notice";

    [JsonPropertyName("formatted_body")]
    public string FormattedBody { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonPropertyName("m.relates_to")]
    public MessageRelatesTo? RelatesTo { get; set; }

    /// <summary>
    /// Media URI for this message, if any
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    public class MessageRelatesTo {
        [JsonPropertyName("m.in_reply_to")]
        public MessageInReplyTo? InReplyTo { get; set; }

        public class MessageInReplyTo {
            [JsonPropertyName("event_id")]
            public string EventId { get; set; }
        }
    }
}
