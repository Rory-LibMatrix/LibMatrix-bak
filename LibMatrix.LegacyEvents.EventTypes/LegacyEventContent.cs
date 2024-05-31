using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes;

public abstract class LegacyEventContent;

public class UnknownLegacyEventContent : TimelineLegacyEventContent;

public abstract class TimelineLegacyEventContent : LegacyEventContent {
    [JsonPropertyName("m.relates_to")]
    public MessageRelatesTo? RelatesTo { get; set; }

    [JsonPropertyName("m.new_content")]
    public JsonObject? NewContent { get; set; }

    public TimelineLegacyEventContent SetReplaceRelation(string eventId) {
        NewContent = JsonSerializer.SerializeToNode(this, GetType())!.AsObject();
        // NewContent = JsonSerializer.Deserialize(jsonText, GetType());
        RelatesTo = new MessageRelatesTo {
            RelationType = "m.replace",
            EventId = eventId
        };
        return this;
    }

    public T SetReplaceRelation<T>(string eventId) where T : TimelineLegacyEventContent => SetReplaceRelation(eventId) as T ?? throw new InvalidOperationException();

    public class MessageRelatesTo {
        [JsonPropertyName("m.in_reply_to")]
        public EventInReplyTo? InReplyTo { get; set; }

        [JsonPropertyName("event_id")]
        public string? EventId { get; set; }

        [JsonPropertyName("rel_type")]
        public string? RelationType { get; set; }

        public class EventInReplyTo {
            [JsonPropertyName("event_id")]
            public string? EventId { get; set; }

            [JsonPropertyName("rel_type")]
            public string? RelType { get; set; }
        }
    }
}