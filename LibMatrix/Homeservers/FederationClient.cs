using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using LibMatrix.Responses;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class FederationClient(string baseUrl) {
    public static async Task<FederationClient?> TryCreate(string baseUrl, string? proxy = null) {
        try {
            return await Create(baseUrl, proxy);
        }
        catch (Exception e) {
            Console.WriteLine($"Failed to create homeserver {baseUrl}: {e.Message}");
            return null;
        }
    }

    public static async Task<FederationClient> Create(string baseUrl, string? proxy = null) {
        var homeserver = new FederationClient(baseUrl);
        homeserver.WellKnownUris = await new HomeserverResolverService().ResolveHomeserverFromWellKnown(baseUrl);
        if(string.IsNullOrWhiteSpace(proxy) && string.IsNullOrWhiteSpace(homeserver.WellKnownUris.Client))
            Console.WriteLine($"Failed to resolve homeserver client URI for {baseUrl}");
        if(string.IsNullOrWhiteSpace(proxy) && string.IsNullOrWhiteSpace(homeserver.WellKnownUris.Server))
            Console.WriteLine($"Failed to resolve homeserver server URI for {baseUrl}");

        if (!string.IsNullOrWhiteSpace(homeserver.WellKnownUris.Server))
            homeserver.HttpClient = new() {
                BaseAddress = new Uri(proxy ?? homeserver.WellKnownUris.Server ?? throw new InvalidOperationException($"Failed to resolve homeserver server URI for {baseUrl}")),
                Timeout = TimeSpan.FromSeconds(120)
            };

        if (proxy is not null) {
            homeserver.HttpClient.DefaultRequestHeaders.Add("MXAE_UPSTREAM", baseUrl);
        }

        return homeserver;
    }
    
    public string BaseUrl { get; } = baseUrl;

    public MatrixHttpClient HttpClient { get; set; } = null!;
    public HomeserverResolverService.WellKnownUris WellKnownUris { get; set; } = null!;

    public async Task<ServerVersionResponse> GetServerVersionAsync() {
        return await HttpClient.GetFromJsonAsync<ServerVersionResponse>("/_matrix/federation/v1/version");
    }

}

public class ServerVersionResponse {
    [JsonPropertyName("server")]
    public required ServerInfo Server { get; set; }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ServerInfo {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}