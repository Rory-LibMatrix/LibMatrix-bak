using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
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
    private static Dictionary<string, AuthenticatedHomeserverGeneric> _authenticatedHomeServerCache = new();

    private static Dictionary<string, SemaphoreSlim> _remoteHomeserverSemaphore = new();
    private static Dictionary<string, RemoteHomeServer> _remoteHomeServerCache = new();

    public async Task<AuthenticatedHomeserverGeneric> GetAuthenticatedWithToken(string homeserver, string accessToken,
        string? proxy = null) {
        var sem = _authenticatedHomeserverSemaphore.GetOrCreate(homeserver + accessToken, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        lock (_authenticatedHomeServerCache) {
            if (_authenticatedHomeServerCache.ContainsKey(homeserver + accessToken)) {
                sem.Release();
                return _authenticatedHomeServerCache[homeserver + accessToken];
            }
        }

        var domain = proxy ?? await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver);

        AuthenticatedHomeserverGeneric hs;
        if (true) {
            hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverMxApiExtended>(homeserver, accessToken);
        }
        else {
            hs = await AuthenticatedHomeserverGeneric.Create<AuthenticatedHomeserverSynapse>(homeserver, accessToken);
        }

        // (() => hs.WhoAmI) = (await hs._httpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami"))!;

        lock(_authenticatedHomeServerCache)
            _authenticatedHomeServerCache[homeserver + accessToken] = hs;
        sem.Release();

        return hs;
    }

    public async Task<RemoteHomeServer> GetRemoteHomeserver(string homeserver, string? proxy = null) {
        var hs = await RemoteHomeServer.Create(proxy ?? await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver));
        // hs._httpClient.Dispose();
        // hs._httpClient = new MatrixHttpClient { BaseAddress = new Uri(hs.FullHomeServerDomain) };
        // hs._httpClient.Timeout = TimeSpan.FromSeconds(120);
        return hs;
    }

    public async Task<LoginResponse> Login(string homeserver, string user, string password, string? proxy = null) {
        var hs = await GetRemoteHomeserver(homeserver, proxy);
        var payload = new LoginRequest {
            Identifier = new LoginRequest.LoginIdentifier { User = user },
            Password = password
        };
        var resp = await hs._httpClient.PostAsJsonAsync("/_matrix/client/v3/login", payload);
        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        return data!;
    }
}
