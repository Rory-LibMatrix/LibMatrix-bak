using Microsoft.Extensions.Configuration;

namespace PluralContactBotPoC.Bot;

public class PluralContactBotConfiguration {
    public PluralContactBotConfiguration(IConfiguration config) => config.GetRequiredSection("PluralContactBot").Bind(this);

    // public string
}
