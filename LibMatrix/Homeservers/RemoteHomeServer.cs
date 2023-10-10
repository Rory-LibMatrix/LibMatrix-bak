using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Extensions;
using LibMatrix.Responses;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class RemoteHomeServer(string baseUrl) {
    public static async Task<RemoteHomeServer> Create(string baseUrl) =>
        new(baseUrl) {
            _httpClient = new() {
                BaseAddress = new Uri(await new HomeserverResolverService().ResolveHomeserverFromWellKnown(baseUrl)
                                      ?? throw new InvalidOperationException("Failed to resolve homeserver")),
                Timeout = TimeSpan.FromSeconds(120)
            }
        };

    private Dictionary<string, object> _profileCache { get; set; } = new();
    public string BaseUrl { get; } = baseUrl;
    public MatrixHttpClient _httpClient { get; set; }

    public async Task<ProfileResponseEventContent> GetProfileAsync(string mxid) {
        if (mxid is null) throw new ArgumentNullException(nameof(mxid));
        if (_profileCache.TryGetValue(mxid, out var value)) {
            if (value is SemaphoreSlim s) await s.WaitAsync();
            if (value is ProfileResponseEventContent p) return p;
        }

        _profileCache[mxid] = new SemaphoreSlim(1);

        var resp = await _httpClient.GetAsync($"/_matrix/client/v3/profile/{mxid}");
        var data = await resp.Content.ReadFromJsonAsync<ProfileResponseEventContent>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("Profile: " + data);
        _profileCache[mxid] = data;

        return data;
    }

    public async Task<ClientVersionsResponse> GetClientVersionsAsync() {
        var resp = await _httpClient.GetAsync($"/_matrix/client/versions");
        var data = await resp.Content.ReadFromJsonAsync<ClientVersionsResponse>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("ClientVersions: " + data);
        return data;
    }

    public async Task<AliasResult> ResolveRoomAliasAsync(string alias) {
        var resp = await _httpClient.GetAsync($"/_matrix/client/v3/directory/room/{alias.Replace("#", "%23")}");
        var data = await resp.Content.ReadFromJsonAsync<AliasResult>();
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("ResolveAlias: " + data.ToJson());
        return data;
    }

#region Authentication

    public async Task<LoginResponse> LoginAsync(string username, string password, string? deviceName = null) {
        var resp = await _httpClient.PostAsJsonAsync("/_matrix/client/r0/login", new {
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
        var resp = await _httpClient.PostAsJsonAsync("/_matrix/client/r0/register", new {
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
}

public class AliasResult {
    [JsonPropertyName("room_id")]
    public string RoomId { get; set; } = null!;

    [JsonPropertyName("servers")]
    public List<string> Servers { get; set; } = null!;
}
