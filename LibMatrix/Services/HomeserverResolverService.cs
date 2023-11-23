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

    private static readonly ConcurrentDictionary<string, WellKnownUris> _wellKnownCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _wellKnownSemaphores = new();

    public async Task<WellKnownUris> ResolveHomeserverFromWellKnown(string homeserver) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        _wellKnownSemaphores.TryAdd(homeserver, new(1, 1));
        await _wellKnownSemaphores[homeserver].WaitAsync();
        if (_wellKnownCache.TryGetValue(homeserver, out var known)) {
            _wellKnownSemaphores[homeserver].Release();
            return known;
        }

        logger?.LogInformation("Resolving homeserver: {}", homeserver);
        var res = new WellKnownUris {
            Client = await _tryResolveFromClientWellknown(homeserver),
            Server = await _tryResolveFromServerWellknown(homeserver)
        };
        _wellKnownCache.TryAdd(homeserver, res);
        _wellKnownSemaphores[homeserver].Release();
        return res;
    }

    private async Task<string?> _tryResolveFromClientWellknown(string homeserver) {
        if (!homeserver.StartsWith("http")) homeserver = "https://" + homeserver;
        try {
            var resp = await _httpClient.GetFromJsonAsync<JsonElement>($"{homeserver}/.well-known/matrix/client");
            var hs = resp.GetProperty("m.homeserver").GetProperty("base_url").GetString();
            return hs;
        }
        catch { }

        logger?.LogInformation("No client well-known...");
        return null;
    }

    private async Task<string?> _tryResolveFromServerWellknown(string homeserver) {
        if (!homeserver.StartsWith("http")) homeserver = "https://" + homeserver;
        try {
            var resp = await _httpClient.GetFromJsonAsync<JsonElement>($"{homeserver}/.well-known/matrix/server");
            var hs = resp.GetProperty("m.server").GetString();
            if (!hs.StartsWithAnyOf("http://", "https://"))
                hs = $"https://{hs}";
            return hs;
        }
        catch { }

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