using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Extensions;
using LibMatrix.Responses;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class RemoteHomeserver(string baseUrl) {
    public static async Task<RemoteHomeserver> Create(string baseUrl, string? proxy = null) {
        var homeserver = new RemoteHomeserver(baseUrl);
        homeserver.WellKnownUris = await new HomeserverResolverService().ResolveHomeserverFromWellKnown(baseUrl);
        homeserver.ClientHttpClient = new() {
            BaseAddress = new Uri(proxy ?? homeserver.WellKnownUris.Client ?? throw new InvalidOperationException("Failed to resolve homeserver")),
            Timeout = TimeSpan.FromSeconds(120)
        };
        homeserver.ServerHttpClient = new() {
            BaseAddress = new Uri(proxy ?? homeserver.WellKnownUris.Server ?? throw new InvalidOperationException("Failed to resolve homeserver")),
            Timeout = TimeSpan.FromSeconds(120)
        };

        if (proxy is not null) {
            homeserver.ClientHttpClient.DefaultRequestHeaders.Add("MXAE_UPSTREAM", baseUrl);
            homeserver.ServerHttpClient.DefaultRequestHeaders.Add("MXAE_UPSTREAM", baseUrl);
        }

        return homeserver;
    }

    private Dictionary<string, object> _profileCache { get; set; } = new();
    public string BaseUrl { get; } = baseUrl;
    public MatrixHttpClient ClientHttpClient { get; set; }
    public MatrixHttpClient ServerHttpClient { get; set; }
    public HomeserverResolverService.WellKnownUris WellKnownUris { get; set; }

    public async Task<UserProfileResponse> GetProfileAsync(string mxid) {
        if (mxid is null) throw new ArgumentNullException(nameof(mxid));
        if (_profileCache.TryGetValue(mxid, out var value)) {
            if (value is SemaphoreSlim s) await s.WaitAsync();
            if (value is UserProfileResponse p) return p;
        }

        _profileCache[mxid] = new SemaphoreSlim(1);

        var resp = await ClientHttpClient.GetAsync($"/_matrix/client/v3/profile/{mxid}");
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
        var text = await resp.Content.ReadAsStringAsync();
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

    public async Task<ServerVersionResponse> GetServerVersionAsync() {
        return await ServerHttpClient.GetFromJsonAsync<ServerVersionResponse>("/_matrix/federation/v1/version");
    }
    
    
    public string? ResolveMediaUri(string? mxcUri) {
        if (mxcUri is null) return null;
        if (mxcUri.StartsWith("https://")) return mxcUri;
        return $"{ClientHttpClient.BaseAddress}/_matrix/media/v3/download/{mxcUri.Replace("mxc://", "")}".Replace("//_matrix", "/_matrix");
    }
}

public class ServerVersionResponse {

    [JsonPropertyName("server")]
    public ServerInfo Server { get; set; }
    
    public class ServerInfo {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}

public class AliasResult {
    [JsonPropertyName("room_id")]
    public string RoomId { get; set; } = null!;

    [JsonPropertyName("servers")]
    public List<string> Servers { get; set; } = null!;
}
