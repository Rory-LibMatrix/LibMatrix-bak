using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State.RoomInfo;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomAvatarLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.avatar";

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("info")]
    public RoomAvatarInfo? Info { get; set; }

    public class RoomAvatarInfo {
        [JsonPropertyName("h")]
        public int? Height { get; set; }

        [JsonPropertyName("w")]
        public int? Width { get; set; }

        [JsonPropertyName("mimetype")]
        public string? MimeType { get; set; }

        [JsonPropertyName("size")]
        public int? Size { get; set; }
    }
}