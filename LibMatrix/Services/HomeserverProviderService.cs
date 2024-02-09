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

        var rhs = await RemoteHomeserver.Create(homeserver, proxy);
        ClientVersionsResponse clientVersions = new();
        try {
            clientVersions = await rhs.GetClientVersionsAsync();
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to get client versions for {homeserver}", homeserver);
        }

        if (proxy is not null)
            logger.LogInformation("Homeserver {homeserver} proxied via {proxy}...", homeserver, proxy);
        logger.LogInformation("{homeserver}: {clientVersions}", homeserver, clientVersions.ToJson());

        ServerVersionResponse serverVersion;
        try {
            serverVersion = serverVersion = await (rhs.FederationClient?.GetServerVersionAsync() ?? Task.FromResult<ServerVersionResponse?>(null)!);
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to get server version for {homeserver}", homeserver);
            sem.Release();
            throw;
        }

        try {
            if (clientVersions.UnstableFeatures.TryGetValue("gay.rory.mxapiextensions.v0", out var a) && a)
                hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverMxApiExtended>(homeserver, accessToken, proxy);
            else {
                if (serverVersion is { Server.Name: "Synapse" })
                    hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverSynapse>(homeserver, accessToken, proxy);
                else
                    hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverGeneric>(homeserver, accessToken, proxy);
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to create authenticated homeserver for {homeserver}", homeserver);
            sem.Release();
            throw;
        }

        if (impersonatedMxid is not null)
            await hs.SetImpersonate(impersonatedMxid);

        lock (AuthenticatedHomeserverCache) {
            AuthenticatedHomeserverCache[cacheKey] = hs;
        }

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

        lock (RemoteHomeserverCache) {
            RemoteHomeserverCache[cacheKey] = hs;
        }

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