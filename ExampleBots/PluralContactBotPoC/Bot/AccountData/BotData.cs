using System.Text.Json.Serialization;
using LibMatrix.EventTypes;
using LibMatrix.Interfaces;

namespace PluralContactBotPoC.Bot.AccountData;
[MatrixEvent(EventName = "gay.rory.plural_contact_bot.bot_config")]
public class BotData : EventContent {
    [JsonPropertyName("control_room")]
    public string ControlRoom { get; set; } = "";

    [JsonPropertyName("log_room")]
    public string? LogRoom { get; set; } = "";

    [JsonPropertyName("policy_room")]
    public string? PolicyRoom { get; set; } = "";
}
