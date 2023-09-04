using System.Collections.Generic;
using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Common;

[MatrixEvent(EventName = "im.ponies.room_emotes")]
public class RoomEmotesEventData : IStateEventType {
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

    public class PackInfo {

    }
}
