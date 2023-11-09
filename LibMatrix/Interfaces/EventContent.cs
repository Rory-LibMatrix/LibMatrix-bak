using System.Text.Json.Serialization;

namespace LibMatrix.Interfaces;

public abstract class EventContent {
    
}
public abstract class TimelineEventContent : EventContent {
    [JsonPropertyName("m.relates_to")]
    public MessageRelatesTo? RelatesTo { get; set; }

    // [JsonPropertyName("m.new_content")]
    // public TimelineEventContent? NewContent { get; set; }

    public class MessageRelatesTo {
        [JsonPropertyName("m.in_reply_to")]
        public EventInReplyTo? InReplyTo { get; set; }

        public class EventInReplyTo {
            [JsonPropertyName("event_id")]
            public string EventId { get; set; }

            [JsonPropertyName("rel_type")]
            public string RelType { get; set; }
        }
    }
}
