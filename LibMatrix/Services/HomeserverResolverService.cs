using System.Text.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverResolverService(ILogger<HomeserverResolverService>? logger = null) {
    private readonly MatrixHttpClient _httpClient = new();

    private static readonly Dictionary<string, (string, string)> _wellKnownCache = new();
    private static readonly Dictionary<string, SemaphoreSlim> _wellKnownSemaphores = new();

    public async Task<(string client, string server)> ResolveHomeserverFromWellKnown(string homeserver) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        // if(!_wellKnownSemaphores.ContainsKey(homeserver))
            // _wellKnownSemaphores[homeserver] = new(1, 1);
        _wellKnownSemaphores.TryAdd(homeserver, new(1, 1));
        await _wellKnownSemaphores[homeserver].WaitAsync();
        if (_wellKnownCache.TryGetValue(homeserver, out var known)) {
            _wellKnownSemaphores[homeserver].Release();
            return known;
        }
        
        logger?.LogInformation("Resolving homeserver: {}", homeserver);
        var res = (
            await _tryResolveFromClientWellknown(homeserver),
            await _tryResolveFromServerWellknown(homeserver)
        );
        _wellKnownCache.Add(homeserver, res!);
        _wellKnownSemaphores[homeserver].Release();
        return res;
    }

    private async Task<string?> _tryResolveFromClientWellknown(string homeserver) {
        if (!homeserver.StartsWith("http")) homeserver = "https://" + homeserver;
        if (await _httpClient.CheckSuccessStatus($"{homeserver}/.well-known/matrix/client")) {
            var resp = await _httpClient.GetFromJsonAsync<JsonElement>($"{homeserver}/.well-known/matrix/client");
            var hs = resp.GetProperty("m.homeserver").GetProperty("base_url").GetString();
            return hs;
        }

        logger?.LogInformation("No client well-known...");
        return null;
    }

    private async Task<string?> _tryResolveFromServerWellknown(string homeserver) {
        if (!homeserver.StartsWith("http")) homeserver = "https://" + homeserver;
        if (await _httpClient.CheckSuccessStatus($"{homeserver}/.well-known/matrix/server")) {
            var resp = await _httpClient.GetFromJsonAsync<JsonElement>($"{homeserver}/.well-known/matrix/server");
            var hs = resp.GetProperty("m.server").GetString();
            if (!hs.StartsWithAnyOf("http://", "https://"))
                hs = $"https://{hs}";
            return hs;
        }

        logger?.LogInformation("No server well-known...");
        return null;
    }

    public async Task<string?> ResolveMediaUri(string homeserver, string mxc) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        if (mxc is null) throw new ArgumentNullException(nameof(mxc));
        if (!mxc.StartsWith("mxc://")) throw new InvalidDataException("mxc must start with mxc://");
        homeserver = (await ResolveHomeserverFromWellKnown(homeserver)).client;
        return mxc.Replace("mxc://", $"{homeserver}/_matrix/media/v3/download/");
    }
}
