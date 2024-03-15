using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Web;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Extensions;
using LibMatrix.Filters;
using LibMatrix.Helpers;
using LibMatrix.Homeservers.Extensions.NamedCaches;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using LibMatrix.Services;
using LibMatrix.Utilities;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverGeneric(string serverName, string accessToken) : RemoteHomeserver(serverName) {
    public static async Task<T> Create<T>(string serverName, string accessToken, string? proxy = null) where T : AuthenticatedHomeserverGeneric =>
        await Create(typeof(T), serverName, accessToken, proxy) as T ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");

    public static async Task<AuthenticatedHomeserverGeneric> Create(Type type, string serverName, string accessToken, string? proxy = null) {
        if (string.IsNullOrWhiteSpace(proxy))
            proxy = null;
        if (!type.IsAssignableTo(typeof(AuthenticatedHomeserverGeneric))) throw new ArgumentException("Type must be a subclass of AuthenticatedHomeserverGeneric", nameof(type));
        var instance = Activator.CreateInstance(type, serverName, accessToken) as AuthenticatedHomeserverGeneric
                       ?? throw new InvalidOperationException($"Failed to create instance of {type.Name}");

        instance.ClientHttpClient = new MatrixHttpClient {
            Timeout = TimeSpan.FromMinutes(15),
            DefaultRequestHeaders = {
                Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
            }
        };
        instance.FederationClient = await FederationClient.TryCreate(serverName, proxy);

        if (string.IsNullOrWhiteSpace(proxy)) {
            var urls = await new HomeserverResolverService().ResolveHomeserverFromWellKnown(serverName);
            instance.ClientHttpClient.BaseAddress = new Uri(urls?.Client ?? throw new InvalidOperationException("Failed to resolve homeserver"));
        }
        else {
            instance.ClientHttpClient.BaseAddress = new Uri(proxy);
            instance.ClientHttpClient.DefaultRequestHeaders.Add("MXAE_UPSTREAM", serverName);
        }

        instance.WhoAmI = await instance.ClientHttpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami");
        instance.NamedCaches = new HsNamedCaches(instance);

        return instance;
    }

    public WhoAmIResponse WhoAmI { get; set; }
    public string UserId => WhoAmI.UserId;
    public string UserLocalpart => UserId.Split(":")[0][1..];
    public string ServerName => UserId.Split(":", 2)[1];

    public string AccessToken { get; set; } = accessToken;

    public HsNamedCaches NamedCaches { get; set; } = null!;

    public GenericRoom GetRoom(string roomId) {
        if (roomId is null || !roomId.StartsWith("!")) throw new ArgumentException("Room ID must start with !", nameof(roomId));
        return new GenericRoom(this, roomId);
    }

    public virtual async Task<List<GenericRoom>> GetJoinedRooms() {
        var roomQuery = await ClientHttpClient.GetAsync("/_matrix/client/v3/joined_rooms");

        var roomsJson = await roomQuery.Content.ReadFromJsonAsync<JsonElement>();
        var rooms = roomsJson.GetProperty("joined_rooms").EnumerateArray().Select(room => GetRoom(room.GetString()!)).ToList();

        return rooms;
    }

    public virtual async Task<string> UploadFile(string fileName, Stream fileStream, string contentType = "application/octet-stream") {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/_matrix/media/v3/upload?filename={fileName}");
        req.Content = new StreamContent(fileStream);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var res = await ClientHttpClient.SendAsync(req);

        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to upload file: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to upload file: {await res.Content.ReadAsStringAsync()}");
        }

        var resJson = await res.Content.ReadFromJsonAsync<JsonElement>();
        return resJson.GetProperty("content_uri").GetString()!;
    }

    public virtual async Task<GenericRoom> CreateRoom(CreateRoomRequest creationEvent, bool returnExistingIfAliasExists = false, bool joinIfAliasExists = false,
        bool inviteIfAliasExists = false) {
        if (returnExistingIfAliasExists) {
            var aliasRes = await ResolveRoomAliasAsync($"#{creationEvent.RoomAliasName}:{ServerName}");
            if (aliasRes?.RoomId != null) {
                var existingRoom = GetRoom(aliasRes.RoomId);
                if (joinIfAliasExists) await existingRoom.JoinAsync();

                if (inviteIfAliasExists) await existingRoom.InviteUsersAsync(creationEvent.Invite ?? new List<string>());

                return existingRoom;
            }
        }

        creationEvent.CreationContent["creator"] = WhoAmI.UserId;
        var res = await ClientHttpClient.PostAsJsonAsync("/_matrix/client/v3/createRoom", creationEvent, new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to create room: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to create room: {await res.Content.ReadAsStringAsync()}");
        }

        var room = GetRoom((await res.Content.ReadFromJsonAsync<JsonObject>())!["room_id"]!.ToString());

        if (creationEvent.Invite is not null)
            await room.InviteUsersAsync(creationEvent.Invite ?? new List<string>());

        return room;
    }

    public virtual async Task Logout() {
        var res = await ClientHttpClient.PostAsync("/_matrix/client/v3/logout", null);
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to logout: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to logout: {await res.Content.ReadAsStringAsync()}");
        }
    }

#region Utility Functions

    public virtual async IAsyncEnumerable<GenericRoom> GetJoinedRoomsByType(string type) {
        var rooms = await GetJoinedRooms();
        var tasks = rooms.Select(async room => {
            var roomType = await room.GetRoomType();
            if (roomType == type) return room;

            return null;
        }).ToAsyncEnumerable();

        await foreach (var result in tasks)
            if (result is not null)
                yield return result;
    }

#endregion

#region Account Data

    public virtual async Task<T> GetAccountDataAsync<T>(string key) =>
        // var res = await _httpClient.GetAsync($"/_matrix/client/v3/user/{UserId}/account_data/{key}");
        // if (!res.IsSuccessStatusCode) {
        //     Console.WriteLine($"Failed to get account data: {await res.Content.ReadAsStringAsync()}");
        //     throw new InvalidDataException($"Failed to get account data: {await res.Content.ReadAsStringAsync()}");
        // }
        //
        // return await res.Content.ReadFromJsonAsync<T>();
        await ClientHttpClient.GetFromJsonAsync<T>($"/_matrix/client/v3/user/{WhoAmI.UserId}/account_data/{key}");

    public virtual async Task<T?> GetAccountDataOrNullAsync<T>(string key) {
        try {
            return await GetAccountDataAsync<T>(key);
        }
        catch (Exception e) {
            return default;
        }
    }

    public virtual async Task SetAccountDataAsync(string key, object data) {
        var res = await ClientHttpClient.PutAsJsonAsync($"/_matrix/client/v3/user/{WhoAmI.UserId}/account_data/{key}", data);
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to set account data: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to set account data: {await res.Content.ReadAsStringAsync()}");
        }
    }

#endregion

    public async Task UpdateProfileAsync(UserProfileResponse? newProfile, bool preserveCustomRoomProfile = true) {
        if (newProfile is null) return;
        Console.WriteLine($"Updating profile for {WhoAmI.UserId} to {newProfile.ToJson(ignoreNull: true)} (preserving room profiles: {preserveCustomRoomProfile})");
        var oldProfile = await GetProfileAsync(WhoAmI.UserId!);
        Dictionary<string, RoomMemberEventContent> expectedRoomProfiles = new();
        var syncHelper = new SyncHelper(this) {
            Filter = new SyncFilter {
                AccountData = new SyncFilter.EventFilter() {
                    Types = new List<string> {
                        "m.room.member"
                    }
                }
            },
            Timeout = 250
        };
        var targetSyncCount = 0;

        if (preserveCustomRoomProfile) {
            var rooms = await GetJoinedRooms();
            var roomProfiles = rooms.Select(GetOwnRoomProfileWithIdAsync).ToAsyncEnumerable();
            targetSyncCount = rooms.Count;
            await foreach (var (roomId, currentRoomProfile) in roomProfiles)
                try {
                    // var currentRoomProfile = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", WhoAmI.UserId!);
                    //build new profiles
                    if (currentRoomProfile.DisplayName == oldProfile.DisplayName) currentRoomProfile.DisplayName = newProfile.DisplayName;

                    if (currentRoomProfile.AvatarUrl == oldProfile.AvatarUrl) currentRoomProfile.AvatarUrl = newProfile.AvatarUrl;

                    currentRoomProfile.Reason = null;

                    expectedRoomProfiles.Add(roomId, currentRoomProfile);
                }
                catch (Exception e) { }

            Console.WriteLine($"Rooms with custom profiles: {string.Join(',', expectedRoomProfiles.Keys)}");
        }

        if (oldProfile.DisplayName != newProfile.DisplayName)
            await ClientHttpClient.PutAsJsonAsync($"/_matrix/client/v3/profile/{WhoAmI.UserId}/displayname", new { displayname = newProfile.DisplayName });
        else
            Console.WriteLine($"Not updating display name because {oldProfile.DisplayName} == {newProfile.DisplayName}");

        if (oldProfile.AvatarUrl != newProfile.AvatarUrl)
            await ClientHttpClient.PutAsJsonAsync($"/_matrix/client/v3/profile/{WhoAmI.UserId}/avatar_url", new { avatar_url = newProfile.AvatarUrl });
        else
            Console.WriteLine($"Not updating avatar URL because {newProfile.AvatarUrl} == {newProfile.AvatarUrl}");

        if (!preserveCustomRoomProfile) return;

        var syncCount = 0;
        await foreach (var sync in syncHelper.EnumerateSyncAsync()) {
            if (sync.Rooms is null) break;
            List<Task> tasks = new();
            foreach (var (roomId, roomData) in sync.Rooms.Join)
                if (roomData.State is { Events.Count: > 0 }) {
                    var incommingRoomProfile =
                        roomData.State?.Events?.FirstOrDefault(x => x.Type == "m.room.member" && x.StateKey == WhoAmI.UserId)?.TypedContent as RoomMemberEventContent;
                    if (incommingRoomProfile is null) continue;
                    if (!expectedRoomProfiles.ContainsKey(roomId)) continue;
                    var targetRoomProfileOverride = expectedRoomProfiles[roomId];
                    var room = GetRoom(roomId);
                    if (incommingRoomProfile.DisplayName != targetRoomProfileOverride.DisplayName || incommingRoomProfile.AvatarUrl != targetRoomProfileOverride.AvatarUrl)
                        tasks.Add(room.SendStateEventAsync("m.room.member", WhoAmI.UserId, targetRoomProfileOverride));
                }

            await Task.WhenAll(tasks);
            await Task.Delay(1000);

            var differenceFound = false;
            if (syncCount++ >= targetSyncCount) {
                var profiles = GetRoomProfilesAsync();
                await foreach (var (roomId, profile) in profiles) {
                    if (!expectedRoomProfiles.ContainsKey(roomId)) {
                        Console.WriteLine($"Skipping profile check for {roomId} because its not in override list?");
                        continue;
                    }

                    var targetRoomProfileOverride = expectedRoomProfiles[roomId];
                    if (profile.DisplayName != targetRoomProfileOverride.DisplayName || profile.AvatarUrl != targetRoomProfileOverride.AvatarUrl) {
                        differenceFound = true;
                        break;
                    }
                }

                if (!differenceFound) return;
            }
        }
    }

    public async IAsyncEnumerable<KeyValuePair<string, RoomMemberEventContent>> GetRoomProfilesAsync() {
        var rooms = await GetJoinedRooms();
        var results = rooms.Select(GetOwnRoomProfileWithIdAsync).ToAsyncEnumerable();
        await foreach (var res in results) yield return res;
    }

    public async Task<RoomIdResponse> JoinRoomAsync(string roomId, List<string> homeservers = null, string? reason = null) {
        var joinUrl = $"/_matrix/client/v3/join/{HttpUtility.UrlEncode(roomId)}";
        Console.WriteLine($"Calling {joinUrl} with {homeservers?.Count ?? 0} via's...");
        if (homeservers == null || homeservers.Count == 0) homeservers = new List<string> { roomId.Split(':')[1] };
        var fullJoinUrl = $"{joinUrl}?server_name=" + string.Join("&server_name=", homeservers);
        var res = await ClientHttpClient.PostAsJsonAsync(fullJoinUrl, new {
            reason
        });
        return await res.Content.ReadFromJsonAsync<RoomIdResponse>() ?? throw new Exception("Failed to join room?");
    }

#region Room Profile Utility

    private async Task<KeyValuePair<string, RoomMemberEventContent>> GetOwnRoomProfileWithIdAsync(GenericRoom room) =>
        new(room.RoomId, await room.GetStateAsync<RoomMemberEventContent>("m.room.member", WhoAmI.UserId!));

#endregion

    public async Task SetImpersonate(string mxid) {
        if (ClientHttpClient.AdditionalQueryParameters.TryGetValue("user_id", out var existingMxid) && existingMxid == mxid && WhoAmI.UserId == mxid) return;
        ClientHttpClient.AdditionalQueryParameters["user_id"] = mxid;
        WhoAmI = await ClientHttpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami");
    }

    /// <summary>
    ///   Upload a filter to the homeserver. Substitutes @me with the user's ID.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<FilterIdResponse> UploadFilterAsync(SyncFilter filter) {
        List<List<string>?> senderLists = [
            filter.AccountData?.Senders,
            filter.AccountData?.NotSenders,
            filter.Presence?.Senders,
            filter.Presence?.NotSenders,
            filter.Room?.AccountData?.Senders,
            filter.Room?.AccountData?.NotSenders,
            filter.Room?.Ephemeral?.Senders,
            filter.Room?.Ephemeral?.NotSenders,
            filter.Room?.State?.Senders,
            filter.Room?.State?.NotSenders,
            filter.Room?.Timeline?.Senders,
            filter.Room?.Timeline?.NotSenders
        ];
        
        foreach (var list in senderLists)
            if (list is { Count: > 0 } && list.Contains("@me")) {
                list.Remove("@me");
                list.Add(UserId);
            }
        
        var resp = await ClientHttpClient.PostAsJsonAsync("/_matrix/client/v3/user/" + UserId + "/filter", filter);
        return await resp.Content.ReadFromJsonAsync<FilterIdResponse>() ?? throw new Exception("Failed to upload filter?");
    }

    public async Task<SyncFilter> GetFilterAsync(string filterId) {
        if (_filterCache.TryGetValue(filterId, out var filter)) return filter;
        var resp = await ClientHttpClient.GetAsync("/_matrix/client/v3/user/" + UserId + "/filter/" + filterId);
        return _filterCache[filterId] = await resp.Content.ReadFromJsonAsync<SyncFilter>() ?? throw new Exception("Failed to get filter?");
    }

    public class FilterIdResponse {
        [JsonPropertyName("filter_id")]
        public required string FilterId { get; set; }
    }

    /// <summary>
    ///   Enumerate all account data per room.
    ///   <b>Warning</b>: This uses /sync!
    /// </summary>
    /// <param name="includeGlobal">Include non-room account data</param>
    /// <returns>Dictionary of room IDs and their account data.</returns>
    /// <exception cref="Exception"></exception>
    public async Task<Dictionary<string, EventList?>> EnumerateAccountDataPerRoom(bool includeGlobal = false) {
        var syncHelper = new SyncHelper(this);
        syncHelper.FilterId = await NamedCaches.FilterCache.GetOrSetValueAsync(CommonSyncFilters.GetAccountDataWithRooms);
        var resp = await syncHelper.SyncAsync();
        if (resp is null) throw new Exception("Sync failed");
        var perRoomAccountData = new Dictionary<string, EventList?>();

        if (includeGlobal)
            perRoomAccountData[""] = resp.AccountData;
        foreach (var (roomId, room) in resp.Rooms?.Join ?? []) perRoomAccountData[roomId] = room.AccountData;

        return perRoomAccountData;
    }

    /// <summary>
    ///   Enumerate all non-room account data.
    ///   <b>Warning</b>: This uses /sync!
    /// </summary>
    /// <returns>All account data.</returns>
    /// <exception cref="Exception"></exception>
    public async Task<EventList?> EnumerateAccountData() {
        var syncHelper = new SyncHelper(this);
        syncHelper.FilterId = await NamedCaches.FilterCache.GetOrSetValueAsync(CommonSyncFilters.GetAccountData);
        var resp = await syncHelper.SyncAsync();
        if (resp is null) throw new Exception("Sync failed");
        return resp.AccountData;
    }

    private Dictionary<string, string>? _namedFilterCache;
    private Dictionary<string, SyncFilter> _filterCache = new();

    public async Task<JsonObject?> GetCapabilitiesAsync() {
        var res = await ClientHttpClient.GetAsync("/_matrix/client/v3/capabilities");
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to get capabilities: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to get capabilities: {await res.Content.ReadAsStringAsync()}");
        }

        return await res.Content.ReadFromJsonAsync<JsonObject>();
    }

    public class HsNamedCaches {
        internal HsNamedCaches(AuthenticatedHomeserverGeneric hs) {
            FileCache = new NamedFileCache(hs);
            FilterCache = new NamedFilterCache(hs);
        }

        public NamedFilterCache FilterCache { get; init; }
        public NamedFileCache FileCache { get; init; }
    }
}