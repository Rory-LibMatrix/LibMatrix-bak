using Microsoft.Extensions.Configuration;

namespace MediaModeratorPoC.Bot;

public class MediaModBotConfiguration {
    public MediaModBotConfiguration(IConfiguration config) => config.GetRequiredSection("MediaMod").Bind(this);

    public List<string> Admins { get; set; } = new();
    public bool DemoMode { get; set; } = false;
}
