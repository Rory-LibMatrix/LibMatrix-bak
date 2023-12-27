using System.Net.Http.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverProviderService(ILogger<HomeserverProviderService> logger) {
    private static readonly Dictionary<string, SemaphoreSlim> AuthenticatedHomeserverSemaphore = new();
    private static readonly Dictionary<string, AuthenticatedHomeserverGeneric> AuthenticatedHomeserverCache = new();

    private static readonly Dictionary<string, SemaphoreSlim> RemoteHomeserverSemaphore = new();
    private static readonly Dictionary<string, RemoteHomeserver> RemoteHomeserverCache = new();

    public async Task<AuthenticatedHomeserverGeneric> GetAuthenticatedWithToken(string homeserver, string accessToken, string? proxy = null, string? impersonatedMxid = null) {
        var cacheKey = homeserver + accessToken + proxy + impersonatedMxid;
        var sem = AuthenticatedHomeserverSemaphore.GetOrCreate(cacheKey, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        AuthenticatedHomeserverGeneric? hs;
        lock (AuthenticatedHomeserverCache) {
            if (AuthenticatedHomeserverCache.TryGetValue(cacheKey, out hs)) {
                sem.Release();
                return hs;
            }
        }

        // var domain = proxy ?? (await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver)).client;

        var rhs = await RemoteHomeserver.Create(homeserver, proxy);
        var clientVersions = await rhs.GetClientVersionsAsync();
        if (proxy is not null)
            logger.LogInformation($"Homeserver {homeserver} proxied via {proxy}...");
        logger.LogInformation($"{homeserver}: " + clientVersions.ToJson());

        if (clientVersions.UnstableFeatures.TryGetValue("gay.rory.mxapiextensions.v0", out bool a) && a)
            hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverMxApiExtended>(homeserver, accessToken, proxy);
        else {
            var serverVersion = await rhs.GetServerVersionAsync();
            if (serverVersion is { Server.Name: "Synapse" })
                hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverSynapse>(homeserver, accessToken, proxy);
            else
                hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverGeneric>(homeserver, accessToken, proxy);
        }

        if(impersonatedMxid is not null)
            await hs.SetImpersonate(impersonatedMxid);
        
        lock (AuthenticatedHomeserverCache)
            AuthenticatedHomeserverCache[cacheKey] = hs;
        sem.Release();

        return hs;
    }

    public async Task<RemoteHomeserver> GetRemoteHomeserver(string homeserver, string? proxy = null) {
        var cacheKey = homeserver + proxy;
        var sem = RemoteHomeserverSemaphore.GetOrCreate(cacheKey, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        RemoteHomeserver? hs;
        lock (RemoteHomeserverCache) {
            if (RemoteHomeserverCache.TryGetValue(cacheKey, out hs)) {
                sem.Release();
                return hs;
            }
        }

        hs = await RemoteHomeserver.Create(homeserver, proxy);

        lock (RemoteHomeserverCache)
            RemoteHomeserverCache[cacheKey] = hs;
        sem.Release();
        
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