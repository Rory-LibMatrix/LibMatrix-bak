using System.Text.Json.Serialization;

namespace MediaModeratorPoC.AccountData;

public class BotData {
    [JsonPropertyName("control_room")]
    public string ControlRoom { get; set; } = "";

    [JsonPropertyName("log_room")]
    public string? LogRoom { get; set; } = "";
}
