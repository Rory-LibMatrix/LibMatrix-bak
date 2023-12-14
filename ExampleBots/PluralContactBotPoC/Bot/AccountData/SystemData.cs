using System.Text.Json.Serialization;
using LibMatrix.EventTypes;
using LibMatrix.Interfaces;

namespace PluralContactBotPoC.Bot.StateEventTypes;

[MatrixEvent(EventName = "gay.rory.plural_contact_bot.system_data")]
public class SystemData : EventContent {
    [JsonPropertyName("control_room")]
    public required string ControlRoom { get; set; }
    [JsonPropertyName("system_members")]
    public List<string> Members { get; set; } = new();
    [JsonPropertyName("dm_space")]
    public string? DmSpace { get; set; }
}
