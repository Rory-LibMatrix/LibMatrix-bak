using System.Collections.Concurrent;
using System.Text.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverResolverService(ILogger<HomeserverResolverService>? logger = null) {
    private readonly MatrixHttpClient _httpClient = new() {
        Timeout = TimeSpan.FromMilliseconds(10000)
    };

    private static readonly ConcurrentDictionary<string, WellKnownUris> WellKnownCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> WellKnownSemaphores = new();

    public async Task<WellKnownUris> ResolveHomeserverFromWellKnown(string homeserver) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        WellKnownSemaphores.TryAdd(homeserver, new SemaphoreSlim(1, 1));
        await WellKnownSemaphores[homeserver].WaitAsync();
        if (WellKnownCache.TryGetValue(homeserver, out var known)) {
            WellKnownSemaphores[homeserver].Release();
            return known;
        }

        logger?.LogInformation("Resolving homeserver: {}", homeserver);
        var res = new WellKnownUris {
            Client = await _tryResolveFromClientWellknown(homeserver),
            Server = await _tryResolveFromServerWellknown(homeserver)
        };
        WellKnownCache.TryAdd(homeserver, res);
        WellKnownSemaphores[homeserver].Release();
        return res;
    }

    private async Task<string?> _tryResolveFromClientWellknown(string homeserver) {
        if (!homeserver.StartsWith("http")) {
            if (await _httpClient.CheckSuccessStatus($"https://{homeserver}/.well-known/matrix/client"))
                homeserver = "https://" + homeserver;
            else if (await _httpClient.CheckSuccessStatus($"http://{homeserver}/.well-known/matrix/client")) {
                homeserver = "http://" + homeserver;
            }
        }

        try {
            var resp = await _httpClient.GetFromJsonAsync<JsonElement>($"{homeserver}/.well-known/matrix/client");
            var hs = resp.GetProperty("m.homeserver").GetProperty("base_url").GetString();
            return hs;
        }
        catch {
            // ignored
        }

        logger?.LogInformation("No client well-known...");
        return null;
    }

    private async Task<string?> _tryResolveFromServerWellknown(string homeserver) {
        if (!homeserver.StartsWith("http")) {
            if (await _httpClient.CheckSuccessStatus($"https://{homeserver}/.well-known/matrix/server"))
                homeserver = "https://" + homeserver;
            else if (await _httpClient.CheckSuccessStatus($"http://{homeserver}/.well-known/matrix/server")) {
                homeserver = "http://" + homeserver;
            }
        }

        try {
            var resp = await _httpClient.GetFromJsonAsync<JsonElement>($"{homeserver}/.well-known/matrix/server");
            var hs = resp.GetProperty("m.server").GetString();
            if (hs is null) throw new InvalidDataException("m.server is null");
            if (!hs.StartsWithAnyOf("http://", "https://"))
                hs = $"https://{hs}";
            return hs;
        }
        catch {
            // ignored
        }

        // fallback: most servers host these on the same location
        var clientUrl = await _tryResolveFromClientWellknown(homeserver);
        if (clientUrl is not null && await _httpClient.CheckSuccessStatus($"{clientUrl}/_matrix/federation/v1/version"))
            return clientUrl;

        logger?.LogInformation("No server well-known...");
        return null;
    }

    public async Task<string?> ResolveMediaUri(string homeserver, string mxc) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        if (mxc is null) throw new ArgumentNullException(nameof(mxc));
        if (!mxc.StartsWith("mxc://")) throw new InvalidDataException("mxc must start with mxc://");
        homeserver = (await ResolveHomeserverFromWellKnown(homeserver)).Client;
        return mxc.Replace("mxc://", $"{homeserver}/_matrix/media/v3/download/");
    }

    public class WellKnownUris {
        public string? Client { get; set; }
        public string? Server { get; set; }
    }
}