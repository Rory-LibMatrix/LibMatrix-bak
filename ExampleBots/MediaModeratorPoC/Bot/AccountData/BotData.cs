using System.Text.Json.Serialization;

namespace MediaModeratorPoC.Bot.AccountData;

public class BotData {
    [JsonPropertyName("control_room")]
    public string ControlRoom { get; set; } = "";

    [JsonPropertyName("log_room")]
    public string? LogRoom { get; set; } = "";

    [JsonPropertyName("policy_room")]
    public string? PolicyRoom { get; set; } = "";
}
