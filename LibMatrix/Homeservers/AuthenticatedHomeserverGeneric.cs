using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Filters;
using LibMatrix.Helpers;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverGeneric(string baseUrl, string accessToken) : RemoteHomeServer(baseUrl) {
    public static async Task<T> Create<T>(string baseUrl, string accessToken) where T : AuthenticatedHomeserverGeneric {
        var instance = Activator.CreateInstance(typeof(T), baseUrl, accessToken) as T
                       ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");
        instance._httpClient = new() {
            BaseAddress = new Uri(await new HomeserverResolverService().ResolveHomeserverFromWellKnown(baseUrl)
                                  ?? throw new InvalidOperationException("Failed to resolve homeserver")),
            Timeout = TimeSpan.FromMinutes(15),
            DefaultRequestHeaders = {
                Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
            }
        };
        instance.WhoAmI = await instance._httpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami");
        return instance;
    }

    // Activator.CreateInstance(baseUrl, accessToken) {
    //     _httpClient = new() {
    //         BaseAddress = new Uri(await new HomeserverResolverService().ResolveHomeserverFromWellKnown(baseUrl)
    //                               ?? throw new InvalidOperationException("Failed to resolve homeserver")),
    //         Timeout = TimeSpan.FromMinutes(15),
    //         DefaultRequestHeaders = {
    //             Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
    //         }
    //     }
    // };

    public WhoAmIResponse? WhoAmI { get; set; }
    public string? UserId => WhoAmI?.UserId;
    public string? UserLocalpart => UserId?.Split(":")[0][1..];
    public string? ServerName => UserId?.Split(":", 2)[1];

    // public virtual async Task<WhoAmIResponse> WhoAmI() {
    // if (_whoAmI is not null) return _whoAmI;
    // _whoAmI = await _httpClient.GetFromJsonAsync<WhoAmIResponse>("/_matrix/client/v3/account/whoami");
    // return _whoAmI;
    // }

    public string AccessToken { get; set; } = accessToken;

    public GenericRoom GetRoom(string roomId) {
        if (roomId is null || !roomId.StartsWith("!")) throw new ArgumentException("Room ID must start with !", nameof(roomId));
        return new GenericRoom(this, roomId);
    }

    public virtual async Task<List<GenericRoom>> GetJoinedRooms() {
        var roomQuery = await _httpClient.GetAsync("/_matrix/client/v3/joined_rooms");

        var roomsJson = await roomQuery.Content.ReadFromJsonAsync<JsonElement>();
        var rooms = roomsJson.GetProperty("joined_rooms").EnumerateArray().Select(room => GetRoom(room.GetString()!)).ToList();

        Console.WriteLine($"Fetched {rooms.Count} rooms");

        return rooms;
    }

    public virtual async Task<string> UploadFile(string fileName, Stream fileStream, string contentType = "application/octet-stream") {
        var res = await _httpClient.PostAsync($"/_matrix/media/v3/upload?filename={fileName}", new StreamContent(fileStream));
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
                if (joinIfAliasExists) {
                    await existingRoom.JoinAsync();
                }

                if (inviteIfAliasExists) {
                    await existingRoom.InviteUsersAsync(creationEvent.Invite ?? new());
                }

                return existingRoom;
            }
        }

        creationEvent.CreationContent["creator"] = WhoAmI.UserId;
        var res = await _httpClient.PostAsJsonAsync("/_matrix/client/v3/createRoom", creationEvent, new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to create room: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to create room: {await res.Content.ReadAsStringAsync()}");
        }

        var room = GetRoom((await res.Content.ReadFromJsonAsync<JsonObject>())!["room_id"]!.ToString());

        if (creationEvent.Invite is not null)
            await room.InviteUsersAsync(creationEvent.Invite ?? new());

        return room;
    }

    public virtual async Task Logout() {
        var res = await _httpClient.PostAsync("/_matrix/client/v3/logout", null);
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
            if (roomType == type) {
                return room;
            }

            return null;
        }).ToAsyncEnumerable();

        await foreach (var result in tasks) {
            if (result is not null) yield return result;
        }
    }

#endregion

#region Account Data

    public virtual async Task<T> GetAccountDataAsync<T>(string key) {
        // var res = await _httpClient.GetAsync($"/_matrix/client/v3/user/{UserId}/account_data/{key}");
        // if (!res.IsSuccessStatusCode) {
        //     Console.WriteLine($"Failed to get account data: {await res.Content.ReadAsStringAsync()}");
        //     throw new InvalidDataException($"Failed to get account data: {await res.Content.ReadAsStringAsync()}");
        // }
        //
        // return await res.Content.ReadFromJsonAsync<T>();
        return await _httpClient.GetFromJsonAsync<T>($"/_matrix/client/v3/user/{WhoAmI.UserId}/account_data/{key}");
    }

    public virtual async Task SetAccountData(string key, object data) {
        var res = await _httpClient.PutAsJsonAsync($"/_matrix/client/v3/user/{WhoAmI.UserId}/account_data/{key}", data);
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to set account data: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to set account data: {await res.Content.ReadAsStringAsync()}");
        }
    }

#endregion

    public string? ResolveMediaUri(string? mxcUri) {
        if (mxcUri is null) return null;
        return $"{_httpClient.BaseAddress}/_matrix/media/v3/download/{mxcUri.Replace("mxc://", "")}".Replace("//", "/");
    }

    public async Task UpdateProfileAsync(ProfileResponseEventContent? newProfile, bool preserveCustomRoomProfile = true) {
        Console.WriteLine($"Updating profile for {WhoAmI.UserId} to {newProfile.ToJson(ignoreNull: true)} (preserving room profiles: {preserveCustomRoomProfile})");
        if (newProfile is null) return;
        var oldProfile = await GetProfileAsync(WhoAmI.UserId!);
        Dictionary<string, RoomMemberEventContent> targetRoomProfileOverrides = new();
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
        int targetSyncCount = 0;

        if (preserveCustomRoomProfile) {
            var rooms = await GetJoinedRooms();
            targetSyncCount = rooms.Count;
            foreach (var room in rooms) {
                try {
                    var currentRoomProfile = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", WhoAmI.UserId!);
                    //build new profiles

                    if (currentRoomProfile.DisplayName == oldProfile.DisplayName) {
                        currentRoomProfile.DisplayName = newProfile.DisplayName;
                    }

                    if (currentRoomProfile.AvatarUrl == oldProfile.AvatarUrl) {
                        currentRoomProfile.AvatarUrl = newProfile.AvatarUrl;
                    }

                    targetRoomProfileOverrides.Add(room.RoomId, currentRoomProfile);
                }
                catch (Exception e) { }
            }

            Console.WriteLine($"Rooms with custom profiles: {string.Join(',', targetRoomProfileOverrides.Keys)}");
        }

        if (oldProfile.DisplayName != newProfile.DisplayName) {
            await _httpClient.PutAsJsonAsync($"/_matrix/client/v3/profile/{WhoAmI.UserId}/displayname", new { displayname = newProfile.DisplayName });
        }
        else {
            Console.WriteLine($"Not updating display name because {oldProfile.DisplayName} == {newProfile.DisplayName}");
        }

        if (oldProfile.AvatarUrl != newProfile.AvatarUrl) {
            await _httpClient.PutAsJsonAsync($"/_matrix/client/v3/profile/{WhoAmI.UserId}/avatar_url", new { avatar_url = newProfile.AvatarUrl });
        }
        else {
            Console.WriteLine($"Not updating avatar URL because {newProfile.AvatarUrl} == {newProfile.AvatarUrl}");
        }

        if (!preserveCustomRoomProfile) return;

        int syncCount = 0;
        await foreach (var sync in syncHelper.EnumerateSyncAsync()) {
            if (sync.Rooms is null) break;
            foreach (var (roomId, roomData) in sync.Rooms.Join) {
                if (roomData.State is { Events: { Count: > 0 } }) {
                    var updatedRoomProfile =
                        roomData.State?.Events?.FirstOrDefault(x => x.Type == "m.room.member" && x.StateKey == WhoAmI.UserId)?.TypedContent as RoomMemberEventContent;
                    if (updatedRoomProfile is null) continue;
                    if (!targetRoomProfileOverrides.ContainsKey(roomId)) continue;
                    var targetRoomProfileOverride = targetRoomProfileOverrides[roomId];
                    var room = GetRoom(roomId);
                    if (updatedRoomProfile.DisplayName != targetRoomProfileOverride.DisplayName || updatedRoomProfile.AvatarUrl != targetRoomProfileOverride.AvatarUrl)
                        await room.SendStateEventAsync("m.room.member", WhoAmI.UserId, targetRoomProfileOverride);
                }
            }

            var differenceFound = false;
            if (syncCount++ >= targetSyncCount) {
                var profiles = GetRoomProfilesAsync();
                await foreach ((string roomId, var profile) in profiles) {
                    if (!targetRoomProfileOverrides.ContainsKey(roomId)) continue;
                    var targetRoomProfileOverride = targetRoomProfileOverrides[roomId];
                    if (profile.DisplayName != targetRoomProfileOverride.DisplayName || profile.AvatarUrl != targetRoomProfileOverride.AvatarUrl) {
                        differenceFound = true;
                        break;
                    }
                }
                // var rooms = await GetJoinedRooms();
                // List<ProfileResponseEventContent> currentProfiles = new();
                // foreach (var room in rooms) {
                //     try {
                //         var roomProfile = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", WhoAmI.UserId!);
                //         currentProfiles.Add(new ProfileResponseEventContent {
                //             AvatarUrl = roomProfile.AvatarUrl,
                //             DisplayName = roomProfile.DisplayName
                //         });
                //     }
                //     catch (Exception e) { }
                // }
                // if (currentProfiles.All(x => x.DisplayName == newProfile.DisplayName) && currentProfiles.All(x => x.AvatarUrl == newProfile.AvatarUrl)) {
                //     Console.WriteLine("All rooms have been updated");
                //     break;
                // }
            }

            if (!differenceFound) return;
        }
    }

    public async IAsyncEnumerable<KeyValuePair<string, RoomMemberEventContent>> GetRoomProfilesAsync() {
        var rooms = await GetJoinedRooms();
        foreach (var room in rooms) {
            RoomMemberEventContent? content = null;
            try {
                content = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", WhoAmI.UserId!);
            }
            catch (Exception e) { }

            if (content is not null)
                yield return new KeyValuePair<string, RoomMemberEventContent>(room.RoomId, content!);
        }
    }
}