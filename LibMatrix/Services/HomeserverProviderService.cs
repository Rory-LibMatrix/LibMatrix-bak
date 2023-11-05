using System.Net.Http.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverProviderService(ILogger<HomeserverProviderService> logger, HomeserverResolverService homeserverResolverService) {
    private static Dictionary<string, SemaphoreSlim> _authenticatedHomeserverSemaphore = new();
    private static Dictionary<string, AuthenticatedHomeserverGeneric> _authenticatedHomeserverCache = new();

    private static Dictionary<string, SemaphoreSlim> _remoteHomeserverSemaphore = new();
    private static Dictionary<string, RemoteHomeserver> _remoteHomeserverCache = new();

    public async Task<AuthenticatedHomeserverGeneric> GetAuthenticatedWithToken(string homeserver, string accessToken, string? proxy = null) {
        var cacheKey = homeserver + accessToken + proxy;
        var sem = _authenticatedHomeserverSemaphore.GetOrCreate(cacheKey, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        AuthenticatedHomeserverGeneric? hs;
        lock (_authenticatedHomeserverCache) {
            if (_authenticatedHomeserverCache.TryGetValue(cacheKey, out hs)) {
                sem.Release();
                return hs;
            }
        }

        // var domain = proxy ?? (await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver)).client;

        var rhs = await RemoteHomeserver.Create(homeserver, proxy);
        var clientVersions = await rhs.GetClientVersionsAsync();
        if(proxy is not null)
            Console.WriteLine($"Homeserver {homeserver} proxied via {proxy}...");
        Console.WriteLine($"{homeserver}: " + clientVersions.ToJson());

        if (clientVersions.UnstableFeatures.TryGetValue("gay.rory.mxapiextensions.v0", out bool a) && a)
            hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverMxApiExtended>(homeserver, accessToken, proxy);
        else {
            var serverVersion = await rhs.GetServerVersionAsync();
            if (serverVersion is { Server.Name: "Synapse" })
                hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverSynapse>(homeserver, accessToken, proxy);
            else
                hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverGeneric>(homeserver, accessToken, proxy);
        }

        lock (_authenticatedHomeserverCache)
            _authenticatedHomeserverCache[cacheKey] = hs;
        sem.Release();

        return hs;
    }

    public async Task<RemoteHomeserver> GetRemoteHomeserver(string homeserver, string? proxy = null) {
        var hs = await RemoteHomeserver.Create(homeserver, proxy);
        // hs._httpClient.Dispose();
        // hs._httpClient = new MatrixHttpClient { BaseAddress = new Uri(hs.ServerName) };
        // hs._httpClient.Timeout = TimeSpan.FromSeconds(120);
        return hs;
    }

    public async Task<LoginResponse> Login(string homeserver, string user, string password, string? proxy = null) {
        var hs = await GetRemoteHomeserver(homeserver, proxy);
        var payload = new LoginRequest {
            Identifier = new LoginRequest.LoginIdentifier { User = user },
            Password = password
        };
        var resp = await hs.ClientHttpClient.PostAsJsonAsync("/_matrix/client/v3/login", payload);
        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        return data!;
    }
}