using System.Collections.Generic;
using System.Text.Json.Serialization;
using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.alias")]
public class RoomAliasEventData : IStateEventType {
    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}
