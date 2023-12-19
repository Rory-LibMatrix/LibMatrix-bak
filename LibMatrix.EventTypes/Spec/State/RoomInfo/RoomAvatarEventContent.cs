using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State.RoomInfo;

[MatrixEvent(EventName = EventId)]
public class RoomAvatarEventContent : EventContent {
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
