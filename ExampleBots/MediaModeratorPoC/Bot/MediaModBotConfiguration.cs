using Microsoft.Extensions.Configuration;

namespace MediaModeratorPoC.Bot;

public class MediaModBotConfiguration {
    public MediaModBotConfiguration(IConfiguration config) {
        config.GetRequiredSection("MediaMod").Bind(this);
    }
    public string Homeserver { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string Prefix { get; set; }
    public List<string> Admins { get; set; } = new();
}
