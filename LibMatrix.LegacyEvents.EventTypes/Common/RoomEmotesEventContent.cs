using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Common;

[MatrixEvent(EventName = EventId)]
public class RoomEmotesEventContent : TimelineEventContent {
    public const string EventId = "im.ponies.room_emotes";

    [JsonPropertyName("emoticons")]
    public Dictionary<string, EmoticonData>? Emoticons { get; set; }

    [JsonPropertyName("images")]
    public Dictionary<string, EmoticonData>? Images { get; set; }

    [JsonPropertyName("pack")]
    public PackInfo? Pack { get; set; }

    public class EmoticonData {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class PackInfo; // TODO: Implement this
}