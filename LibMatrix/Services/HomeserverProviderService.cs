using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Services;

public class HomeserverProviderService {
    private readonly TieredStorageService _tieredStorageService;
    private readonly ILogger<HomeserverProviderService> _logger;
    private readonly HomeserverResolverService _homeserverResolverService;

    public HomeserverProviderService(TieredStorageService tieredStorageService,
        ILogger<HomeserverProviderService> logger, HomeserverResolverService homeserverResolverService) {
        _tieredStorageService = tieredStorageService;
        _logger = logger;
        _homeserverResolverService = homeserverResolverService;
        logger.LogDebug("New HomeserverProviderService created with TieredStorageService<{}>!",
            string.Join(", ", tieredStorageService.GetType().GetProperties().Select(x => x.Name)));
    }

    private static Dictionary<string, SemaphoreSlim> _authenticatedHomeserverSemaphore = new();
    private static Dictionary<string, AuthenticatedHomeserverGeneric> _authenticatedHomeServerCache = new();

    public async Task<AuthenticatedHomeserverGeneric> GetAuthenticatedWithToken(string homeserver, string accessToken,
        string? proxy = null) {
        var sem = _authenticatedHomeserverSemaphore.GetOrCreate(homeserver + accessToken, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        if (_authenticatedHomeServerCache.ContainsKey(homeserver + accessToken)) {
            sem.Release();
            return _authenticatedHomeServerCache[homeserver + accessToken];
        }

        var domain = proxy ?? await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver);
        var hc = new MatrixHttpClient { BaseAddress = new Uri(domain) };

        AuthenticatedHomeserverGeneric hs;
        if (true) {
            hs = new AuthenticatedHomeserverMxApiExtended(_tieredStorageService, homeserver, accessToken);
        }
        else {
            hs = new AuthenticatedHomeserverGeneric(_tieredStorageService, homeserver, accessToken);
        }

        hs.FullHomeServerDomain = domain;
        hs._httpClient = hc;
        hs._httpClient.Timeout = TimeSpan.FromMinutes(15);
        hs._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        hs.WhoAmI = (await hs._httpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami"))!;

        lock(_authenticatedHomeServerCache)
            _authenticatedHomeServerCache[homeserver + accessToken] = hs;
        sem.Release();

        return hs;
    }

    public async Task<RemoteHomeServer> GetRemoteHomeserver(string homeserver, string? proxy = null) {
        var hs = new RemoteHomeServer(homeserver);
        hs.FullHomeServerDomain = proxy ?? await _homeserverResolverService.ResolveHomeserverFromWellKnown(homeserver);
        hs._httpClient.Dispose();
        hs._httpClient = new MatrixHttpClient { BaseAddress = new Uri(hs.FullHomeServerDomain) };
        hs._httpClient.Timeout = TimeSpan.FromSeconds(120);
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
