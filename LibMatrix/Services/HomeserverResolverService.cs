using System.Text.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverResolverService(ILogger<HomeserverResolverService>? logger = null) {
    private readonly MatrixHttpClient _httpClient = new();

    private static readonly Dictionary<string, string> _wellKnownCache = new();
    private static readonly Dictionary<string, SemaphoreSlim> _wellKnownSemaphores = new();

    public async Task<string> ResolveHomeserverFromWellKnown(string homeserver) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        if(_wellKnownCache.TryGetValue(homeserver, out var known)) return known;
        logger?.LogInformation("Resolving homeserver: {}", homeserver);
        var res = await _resolveHomeserverFromWellKnown(homeserver);
        if (!res.StartsWith("http")) res = "https://" + res;
        if (res.EndsWith(":443")) res = res[..^4];
        return res;
    }

    private async Task<string> _resolveHomeserverFromWellKnown(string homeserver) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        var sem = _wellKnownSemaphores.GetOrCreate(homeserver, _ => new SemaphoreSlim(1, 1));
        if(_wellKnownCache.TryGetValue(homeserver, out var wellKnown)) return wellKnown;
        await sem.WaitAsync();
        if (_wellKnownCache.TryGetValue(homeserver, out var known)) {
            sem.Release();
            return known;
        }

        string? result = null;
        logger?.LogInformation("Attempting to resolve homeserver: {}", homeserver);
        result ??= await _tryResolveFromClientWellknown(homeserver);
        result ??= await _tryResolveFromServerWellknown(homeserver);
        result ??= await _tryCheckIfDomainHasHomeserver(homeserver);

        if (result is null) throw new InvalidDataException($"Failed to resolve homeserver for {homeserver}! Is it online and configured correctly?");

        //success!
        logger?.LogInformation("Resolved homeserver: {} -> {}", homeserver, result);
        _wellKnownCache[homeserver] = result;
        sem.Release();
        return result;
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
            return hs;
        }

        logger?.LogInformation("No server well-known...");
        return null;
    }

    private async Task<string?> _tryCheckIfDomainHasHomeserver(string homeserver) {
        logger?.LogInformation("Checking if {} hosts a homeserver...", homeserver);
        if (await _httpClient.CheckSuccessStatus($"{homeserver}/_matrix/client/versions"))
            return homeserver;
        logger?.LogInformation("No homeserver on shortname...");
        return null;
    }

    private async Task<string?> _tryCheckIfSubDomainHasHomeserver(string homeserver, string subdomain) {
        homeserver = homeserver.Replace("https://", $"https://{subdomain}.");
        return await _tryCheckIfDomainHasHomeserver(homeserver);
    }

    public async Task<string?> ResolveMediaUri(string homeserver, string mxc) {
        if (homeserver is null) throw new ArgumentNullException(nameof(homeserver));
        if (mxc is null) throw new ArgumentNullException(nameof(mxc));
        if (!mxc.StartsWith("mxc://")) throw new InvalidDataException("mxc must start with mxc://");
        homeserver = await ResolveHomeserverFromWellKnown(homeserver);
        return mxc.Replace("mxc://", $"{homeserver}/_matrix/media/v3/download/");
    }
}
