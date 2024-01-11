using Microsoft.Extensions.Configuration;

namespace LibMatrix.ExampleBot.Bot;

public class DevTestBotConfiguration {
    public DevTestBotConfiguration(IConfiguration config) {
        config.GetRequiredSection("Bot").Bind(this);
    }
    public string Homeserver { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string Prefix { get; set; }
}
