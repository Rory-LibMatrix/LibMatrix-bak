using Microsoft.Extensions.Configuration;

namespace TestDataGenerator.Bot;

public class DataFetcherConfiguration {
    public DataFetcherConfiguration(IConfiguration config) => config.GetRequiredSection("DataFetcher").Bind(this);

    // public string
}