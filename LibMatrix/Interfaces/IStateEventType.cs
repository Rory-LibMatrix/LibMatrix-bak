using System.Text.Json.Serialization;

namespace LibMatrix.Interfaces;

public abstract class EventContent {
    [JsonPropertyName("m.relates_to")]
    public MessageRelatesTo? RelatesTo { get; set; }

    [JsonPropertyName("m.new_content")]
    public EventContent? NewContent { get; set; }

    public abstract class MessageRelatesTo {
        [JsonPropertyName("m.in_reply_to")]
        public EventInReplyTo? InReplyTo { get; set; }



        public abstract class EventInReplyTo {
            [JsonPropertyName("event_id")]
            public string EventId { get; set; }
        }
    }
}
