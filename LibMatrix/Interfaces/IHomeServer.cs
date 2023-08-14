using System.Net.Http.Json;
using LibMatrix.Extensions;
using LibMatrix.StateEventTypes.Spec;

namespace LibMatrix.Interfaces;

public class IHomeServer {
    private readonly Dictionary<string, object> _profileCache = new();
    public string HomeServerDomain { get; set; }
    public string FullHomeServerDomain { get; set; }

    public MatrixHttpClient _httpClient { get; set; } = new();

    public async Task<ProfileResponseEventData> GetProfile(string mxid) {
        if(mxid is null) throw new ArgumentNullException(nameof(mxid));
        if (_profileCache.ContainsKey(mxid)) {
            if (_profileCache[mxid] is SemaphoreSlim s) await s.WaitAsync();
            if (_profileCache[mxid] is ProfileResponseEventData p) return p;
        }
        _profileCache[mxid] = new SemaphoreSlim(1);

        var resp = await _httpClient.GetAsync($"/_matrix/client/v3/profile/{mxid}");
        var data = await resp.Content.ReadFromJsonAsync<ProfileResponseEventData>();
        if (!resp.IsSuccessStatusCode) Console.WriteLine("Profile: " + data);
        _profileCache[mxid] = data;

        return data;
    }
}
