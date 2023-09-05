using System.Net.Http.Json;
using LibMatrix.Extensions;
using LibMatrix.Responses;
using LibMatrix.StateEventTypes.Spec;

namespace LibMatrix.Homeservers;

public class RemoteHomeServer {
    public RemoteHomeServer(string canonicalHomeServerDomain) {
        HomeServerDomain = canonicalHomeServerDomain;
        _httpClient = new MatrixHttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    private Dictionary<string, object> _profileCache { get; set; } = new();
    public string HomeServerDomain { get; set; }
    public string FullHomeServerDomain { get; set; }
    public MatrixHttpClient _httpClient { get; set; }

    public async Task<ProfileResponseEventData> GetProfile(string mxid) {
        if(mxid is null) throw new ArgumentNullException(nameof(mxid));
        if (_profileCache.TryGetValue(mxid, out var value)) {
            if (value is SemaphoreSlim s) await s.WaitAsync();
            if (value is ProfileResponseEventData p) return p;
        }
        _profileCache[mxid] = new SemaphoreSlim(1);

        var resp = await _httpClient.GetAsync($"/_matrix/client/v3/profile/{mxid}");
        var data = await resp.Content.ReadFromJsonAsync<ProfileResponseEventData>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("Profile: " + data);
        _profileCache[mxid] = data;

        return data;
    }

    public async Task<ClientVersionsResponse> GetClientVersions() {
        var resp = await _httpClient.GetAsync($"/_matrix/client/versions");
        var data = await resp.Content.ReadFromJsonAsync<ClientVersionsResponse>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("ClientVersions: " + data);
        return data;
    }
}
