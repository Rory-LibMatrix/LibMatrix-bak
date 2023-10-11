using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
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
            if (aliasRes is not null) {
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
}
