using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ArcaneLibs.Extensions;
using LibMatrix.Extensions;
using LibMatrix.Responses;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class RemoteHomeserver(string baseUrl) {
    public static async Task<RemoteHomeserver?> TryCreate(string baseUrl, string? proxy = null) {
        try {
            return await Create(baseUrl, proxy);
        }
        catch (Exception e) {
            Console.WriteLine($"Failed to create homeserver {baseUrl}: {e.Message}");
            return null;
        }
    }

    public static async Task<RemoteHomeserver> Create(string baseUrl, string? proxy = null) {
        if (string.IsNullOrWhiteSpace(proxy))
            proxy = null;
        var homeserver = new RemoteHomeserver(baseUrl);
        homeserver.WellKnownUris = await new HomeserverResolverService().ResolveHomeserverFromWellKnown(baseUrl);
        if (string.IsNullOrWhiteSpace(homeserver.WellKnownUris.Client))
            Console.WriteLine($"Failed to resolve homeserver client URI for {baseUrl}");
        if (string.IsNullOrWhiteSpace(homeserver.WellKnownUris.Server))
            Console.WriteLine($"Failed to resolve homeserver server URI for {baseUrl}");

        Console.WriteLine(homeserver.WellKnownUris.ToJson(ignoreNull: false));

        homeserver.ClientHttpClient = new MatrixHttpClient {
            BaseAddress = new Uri(proxy ?? homeserver.WellKnownUris.Client ?? throw new InvalidOperationException($"Failed to resolve homeserver client URI for {baseUrl}")),
            Timeout = TimeSpan.FromSeconds(120)
        };

        homeserver.FederationClient = await FederationClient.TryCreate(baseUrl, proxy);

        if (proxy is not null) homeserver.ClientHttpClient.DefaultRequestHeaders.Add("MXAE_UPSTREAM", baseUrl);

        return homeserver;
    }

    private Dictionary<string, object> _profileCache { get; set; } = new();
    public string BaseUrl { get; } = baseUrl;

    public MatrixHttpClient ClientHttpClient { get; set; } = null!;
    public FederationClient? FederationClient { get; set; }
    public HomeserverResolverService.WellKnownUris WellKnownUris { get; set; } = null!;

    public async Task<UserProfileResponse> GetProfileAsync(string mxid, bool useCache = false) {
        if (mxid is null) throw new ArgumentNullException(nameof(mxid));
        if (useCache && _profileCache.TryGetValue(mxid, out var value)) {
            if (value is SemaphoreSlim s) await s.WaitAsync();
            if (value is UserProfileResponse p) return p;
        }

        _profileCache[mxid] = new SemaphoreSlim(1);

        var resp = await ClientHttpClient.GetAsync($"/_matrix/client/v3/profile/{HttpUtility.UrlEncode(mxid)}");
        var data = await resp.Content.ReadFromJsonAsync<UserProfileResponse>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("Profile: " + data);
        _profileCache[mxid] = data;

        return data;
    }

    public async Task<ClientVersionsResponse> GetClientVersionsAsync() {
        var resp = await ClientHttpClient.GetAsync($"/_matrix/client/versions");
        var data = await resp.Content.ReadFromJsonAsync<ClientVersionsResponse>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("ClientVersions: " + data);
        return data;
    }

    public async Task<AliasResult> ResolveRoomAliasAsync(string alias) {
        var resp = await ClientHttpClient.GetAsync($"/_matrix/client/v3/directory/room/{alias.Replace("#", "%23")}");
        var data = await resp.Content.ReadFromJsonAsync<AliasResult>();
        //var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("ResolveAlias: " + data.ToJson());
        return data;
    }

#region Authentication

    public async Task<LoginResponse> LoginAsync(string username, string password, string? deviceName = null) {
        var resp = await ClientHttpClient.PostAsJsonAsync("/_matrix/client/r0/login", new {
            type = "m.login.password",
            identifier = new {
                type = "m.id.user",
                user = username
            },
            password = password,
            initial_device_display_name = deviceName
        });
        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("Login: " + data.ToJson());
        return data;
    }

    public async Task<LoginResponse> RegisterAsync(string username, string password, string? deviceName = null) {
        var resp = await ClientHttpClient.PostAsJsonAsync("/_matrix/client/r0/register", new {
            kind = "user",
            auth = new {
                type = "m.login.dummy"
            },
            username,
            password,
            initial_device_display_name = deviceName ?? "LibMatrix"
        }, new JsonSerializerOptions() {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("Register: " + data.ToJson());
        return data;
    }

#endregion

    public string? ResolveMediaUri(string? mxcUri) {
        if (mxcUri is null) return null;
        if (mxcUri.StartsWith("https://")) return mxcUri;
        return $"{ClientHttpClient.BaseAddress}/_matrix/media/v3/download/{mxcUri.Replace("mxc://", "")}".Replace("//_matrix", "/_matrix");
    }
}

public class AliasResult {
    [JsonPropertyName("room_id")]
    public string RoomId { get; set; } = null!;

    [JsonPropertyName("servers")]
    public List<string> Servers { get; set; } = null!;
}