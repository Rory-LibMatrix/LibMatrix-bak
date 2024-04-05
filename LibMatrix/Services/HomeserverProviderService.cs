using System.Net.Http.Json;
using ArcaneLibs.Collections;
using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverProviderService(ILogger<HomeserverProviderService> logger, HomeserverResolverService hsResolver) {
    private static SemaphoreCache<AuthenticatedHomeserverGeneric> AuthenticatedHomeserverCache = new();
    private static SemaphoreCache<RemoteHomeserver> RemoteHomeserverCache = new();

    public async Task<AuthenticatedHomeserverGeneric> GetAuthenticatedWithToken(string homeserver, string accessToken, string? proxy = null, string? impersonatedMxid = null) {
        return await AuthenticatedHomeserverCache.GetOrAdd($"{homeserver}{accessToken}{proxy}{impersonatedMxid}", async () => {
            var wellKnownUris = await hsResolver.ResolveHomeserverFromWellKnown(homeserver);
            var rhs = new RemoteHomeserver(homeserver, wellKnownUris, ref proxy);

            ClientVersionsResponse? clientVersions = new();
            try {
                clientVersions = await rhs.GetClientVersionsAsync();
            }
            catch (Exception e) {
                logger.LogError(e, "Failed to get client versions for {homeserver}", homeserver);
            }

            ServerVersionResponse? serverVersion;
            try {
                serverVersion = await (rhs.FederationClient?.GetServerVersionAsync() ?? Task.FromResult<ServerVersionResponse?>(null)!);
            }
            catch (Exception e) {
                logger.LogWarning(e, "Failed to get server version for {homeserver}", homeserver);
                throw;
            }

            AuthenticatedHomeserverGeneric hs;
            try {
                if (clientVersions.UnstableFeatures.TryGetValue("gay.rory.mxapiextensions.v0", out var a) && a)
                    hs = new AuthenticatedHomeserverMxApiExtended(homeserver, wellKnownUris, ref proxy, accessToken);
                else {
                    if (serverVersion is { Server.Name: "Synapse" })
                        hs = new AuthenticatedHomeserverSynapse(homeserver, wellKnownUris, ref proxy, accessToken);
                    else
                        hs = new AuthenticatedHomeserverGeneric(homeserver, wellKnownUris, ref proxy, accessToken);
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Failed to create authenticated homeserver for {homeserver}", homeserver);
                throw;
            }

            await hs.Initialise();

            if (impersonatedMxid is not null)
                await hs.SetImpersonate(impersonatedMxid);

            return hs;
        });
    }

    public async Task<RemoteHomeserver> GetRemoteHomeserver(string homeserver, string? proxy = null) =>
        await RemoteHomeserverCache.GetOrAdd($"{homeserver}{proxy}", async () => {
            return new RemoteHomeserver(homeserver, await hsResolver.ResolveHomeserverFromWellKnown(homeserver), ref proxy);
        });

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