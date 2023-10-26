using System.Net.Http.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverProviderService {
    private readonly ILogger<HomeserverProviderService> _logger;
    private readonly HomeserverResolverService _homeserverResolverService;

    public HomeserverProviderService(ILogger<HomeserverProviderService> logger, HomeserverResolverService homeserverResolverService) {
        _logger = logger;
        _homeserverResolverService = homeserverResolverService;
    }

    private static Dictionary<string, SemaphoreSlim> _authenticatedHomeserverSemaphore = new();
    private static Dictionary<string, AuthenticatedHomeserverGeneric> _authenticatedHomeserverCache = new();

    private static Dictionary<string, SemaphoreSlim> _remoteHomeserverSemaphore = new();
    private static Dictionary<string, RemoteHomeserver> _remoteHomeserverCache = new();

    public async Task<AuthenticatedHomeserverGeneric> GetAuthenticatedWithToken(string homeserver, string accessToken, string? proxy = null) {
        var sem = _authenticatedHomeserverSemaphore.GetOrCreate(homeserver + accessToken, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        lock (_authenticatedHomeserverCache) {
            if (_authenticatedHomeserverCache.ContainsKey(homeserver + accessToken)) {
                sem.Release();
                return _authenticatedHomeserverCache[homeserver + accessToken];
            }
        }

        // var domain = proxy ?? (await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver)).client;

        var rhs = await RemoteHomeserver.Create(homeserver);
        var serverVersion = await rhs.GetServerVersionAsync();
        

        AuthenticatedHomeserverGeneric hs;
        if (true) {
            hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverMxApiExtended>(homeserver, accessToken);
        }
        else {
            hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverSynapse>(homeserver, accessToken);
        }

        // (() => hs.WhoAmI) = (await hs._httpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami"))!;

        lock (_authenticatedHomeserverCache)
            _authenticatedHomeserverCache[homeserver + accessToken] = hs;
        sem.Release();

        return hs;
    }

    public async Task<RemoteHomeserver> GetRemoteHomeserver(string homeserver, string? proxy = null) {
        var hs = await RemoteHomeserver.Create(proxy ?? homeserver);
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