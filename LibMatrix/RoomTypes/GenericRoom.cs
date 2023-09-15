using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using LibMatrix.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.StateEventTypes.Spec;
using Microsoft.Extensions.Logging;

namespace LibMatrix.RoomTypes;

public class GenericRoom {
    internal readonly AuthenticatedHomeserverGeneric Homeserver;
    internal readonly MatrixHttpClient _httpClient;

    public GenericRoom(AuthenticatedHomeserverGeneric homeserver, string roomId) {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("Room ID cannot be null or whitespace", nameof(roomId));
        Homeserver = homeserver;
        _httpClient = homeserver._httpClient;
        RoomId = roomId;
        if (GetType() != typeof(SpaceRoom))
            AsSpace = new SpaceRoom(homeserver, RoomId);
    }

    public string RoomId { get; set; }

    [Obsolete("", true)]
    public async Task<JsonElement?> GetStateAsync(string type, string stateKey = "") {
        var url = $"/_matrix/client/v3/rooms/{RoomId}/state";
        if (!string.IsNullOrEmpty(type)) url += $"/{type}";
        if (!string.IsNullOrEmpty(stateKey)) url += $"/{stateKey}";
        return await _httpClient.GetFromJsonAsync<JsonElement>(url);
    }

    public async IAsyncEnumerable<StateEventResponse?> GetFullStateAsync() {
        var res = await _httpClient.GetAsync($"/_matrix/client/v3/rooms/{RoomId}/state");
        var result =
            JsonSerializer.DeserializeAsyncEnumerable<StateEventResponse>(await res.Content.ReadAsStreamAsync());
        await foreach (var resp in result) {
            yield return resp;
        }
    }

    public async Task<T?> GetStateAsync<T>(string type, string stateKey = "") {
        var url = $"/_matrix/client/v3/rooms/{RoomId}/state";
        if (!string.IsNullOrEmpty(type)) url += $"/{type}";
        if (!string.IsNullOrEmpty(stateKey)) url += $"/{stateKey}";
        try {
#if DEBUG && false
            var resp = await _httpClient.GetFromJsonAsync<JsonObject>(url);
            try {
                _homeServer._httpClient.PostAsJsonAsync(
                    "http://localhost:5116/validate/" + typeof(T).AssemblyQualifiedName, resp);
            }
            catch (Exception e) {
                Console.WriteLine("[!!] Checking state response failed: " + e);
            }

            return resp.Deserialize<T>();
#else
            var resp = await _httpClient.GetFromJsonAsync<T>(url);
            return resp;
#endif
        }
        catch (MatrixException e) {
            if (e is not { ErrorCode: "M_NOT_FOUND" }) {
                throw;
            }

            Console.WriteLine(e);
            return default;
        }
    }

    public async Task<MessagesResponse> GetMessagesAsync(string from = "", int limit = 10, string dir = "b",
        string filter = "") {
        var url = $"/_matrix/client/v3/rooms/{RoomId}/messages?from={from}&limit={limit}&dir={dir}";
        if (!string.IsNullOrEmpty(filter)) url += $"&filter={filter}";
        var res = await _httpClient.GetFromJsonAsync<MessagesResponse>(url);
        return res ?? new MessagesResponse();
    }

    // TODO: should we even error handle here?
    public async Task<string> GetNameAsync() {
        try {
            var res = await GetStateAsync<RoomNameEventContent>("m.room.name");
            return res?.Name ?? RoomId;
        }
        catch (MatrixException e) {
            return $"{RoomId} ({e.ErrorCode})";
        }
    }

    public async Task JoinAsync(string[]? homeservers = null, string? reason = null) {
        var join_url = $"/_matrix/client/v3/join/{HttpUtility.UrlEncode(RoomId)}";
        Console.WriteLine($"Calling {join_url} with {homeservers?.Length ?? 0} via's...");
        if (homeservers == null || homeservers.Length == 0) homeservers = new[] { RoomId.Split(':')[1] };
        var fullJoinUrl = $"{join_url}?server_name=" + string.Join("&server_name=", homeservers);
        var res = await _httpClient.PostAsJsonAsync(fullJoinUrl, new {
            reason
        });
    }

    // TODO: rewrite (members endpoint?)
    public async IAsyncEnumerable<StateEventResponse> GetMembersAsync(bool joinedOnly = true) {
        // var res = GetFullStateAsync();
        // await foreach (var member in res) {
        //     if (member?.Type != "m.room.member") continue;
        //     if (joinedOnly && (member.TypedContent as RoomMemberEventContent)?.Membership is not "join") continue;
        //     yield return member;
        // }
        var res = await _httpClient.GetAsync($"/_matrix/client/v3/rooms/{RoomId}/members");
        var result =
            JsonSerializer.DeserializeAsyncEnumerable<StateEventResponse>(await res.Content.ReadAsStreamAsync());
        await foreach (var resp in result) {
            if (resp?.Type != "m.room.member") continue;
            if (joinedOnly && (resp.TypedContent as RoomMemberEventContent)?.Membership is not "join") continue;
            yield return resp;
        }
    }

#region Utility shortcuts

    public async Task<List<string>> GetAliasesAsync() {
        var res = await GetStateAsync<RoomAliasEventContent>("m.room.aliases");
        return res.Aliases;
    }

    public async Task<CanonicalAliasEventContent?> GetCanonicalAliasAsync() =>
        await GetStateAsync<CanonicalAliasEventContent>("m.room.canonical_alias");

    public async Task<RoomTopicEventContent?> GetTopicAsync() =>
        await GetStateAsync<RoomTopicEventContent>("m.room.topic");

    public async Task<RoomAvatarEventContent?> GetAvatarUrlAsync() =>
        await GetStateAsync<RoomAvatarEventContent>("m.room.avatar");

    public async Task<JoinRulesEventContent> GetJoinRuleAsync() =>
        await GetStateAsync<JoinRulesEventContent>("m.room.join_rules");

    public async Task<HistoryVisibilityEventContent?> GetHistoryVisibilityAsync() =>
        await GetStateAsync<HistoryVisibilityEventContent>("m.room.history_visibility");

    public async Task<GuestAccessEventContent?> GetGuestAccessAsync() =>
        await GetStateAsync<GuestAccessEventContent>("m.room.guest_access");

    public async Task<RoomCreateEventContent> GetCreateEventAsync() =>
        await GetStateAsync<RoomCreateEventContent>("m.room.create");

    public async Task<string?> GetRoomType() {
        var res = await GetStateAsync<RoomCreateEventContent>("m.room.create");
        return res.Type;
    }

    public async Task<RoomPowerLevelEventContent?> GetPowerLevelsAsync() =>
        await GetStateAsync<RoomPowerLevelEventContent>("m.room.power_levels");

#endregion

    public async Task ForgetAsync() =>
        await _httpClient.PostAsync($"/_matrix/client/v3/rooms/{RoomId}/forget", null);

    public async Task LeaveAsync(string? reason = null) =>
        await _httpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/leave", new {
            reason
        });

    public async Task KickAsync(string userId, string? reason = null) =>
        await _httpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/kick",
            new UserIdAndReason { UserId = userId, Reason = reason });

    public async Task BanAsync(string userId, string? reason = null) =>
        await _httpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/ban",
            new UserIdAndReason { UserId = userId, Reason = reason });

    public async Task UnbanAsync(string userId) =>
        await _httpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/unban",
            new UserIdAndReason { UserId = userId });

    public async Task<EventIdResponse> SendStateEventAsync(string eventType, object content) =>
        await (await _httpClient.PutAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/state/{eventType}", content))
            .Content.ReadFromJsonAsync<EventIdResponse>();

    public async Task<EventIdResponse> SendStateEventAsync(string eventType, string stateKey, object content) =>
        await (await _httpClient.PutAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/state/{eventType}/{stateKey}", content))
            .Content.ReadFromJsonAsync<EventIdResponse>();

    public async Task<EventIdResponse> SendMessageEventAsync(string eventType, RoomMessageEventContent content) {
        var res = await _httpClient.PutAsJsonAsync(
            $"/_matrix/client/v3/rooms/{RoomId}/send/{eventType}/" + Guid.NewGuid(), content, new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        var resu = await res.Content.ReadFromJsonAsync<EventIdResponse>();
        return resu;
    }

    public async Task<EventIdResponse> SendFileAsync(string eventType, string fileName, Stream fileStream) {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var res = await
            (
                await _httpClient.PutAsync(
                    $"/_matrix/client/v3/rooms/{RoomId}/send/{eventType}/" + Guid.NewGuid(),
                    content
                )
            )
            .Content.ReadFromJsonAsync<EventIdResponse>();
        return res;
    }

    public async Task<T> GetRoomAccountData<T>(string key) {
        var res = await _httpClient.GetAsync($"/_matrix/client/v3/user/{Homeserver.UserId}/rooms/{RoomId}/account_data/{key}");
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to get room account data: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to get room account data: {await res.Content.ReadAsStringAsync()}");
        }

        return await res.Content.ReadFromJsonAsync<T>();
    }

    public async Task SetRoomAccountData(string key, object data) {
        var res = await _httpClient.PutAsJsonAsync($"/_matrix/client/v3/user/{Homeserver.UserId}/rooms/{RoomId}/account_data/{key}", data);
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to set room account data: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to set room account data: {await res.Content.ReadAsStringAsync()}");
        }
    }

    public readonly SpaceRoom AsSpace;

    public async Task<T> GetEvent<T>(string eventId) {
        return await _httpClient.GetFromJsonAsync<T>($"/_matrix/client/v3/rooms/{RoomId}/event/{eventId}");
    }

    public async Task<EventIdResponse> RedactEventAsync(string eventToRedact, string reason) {
        var data = new { reason };
        return (await (await _httpClient.PutAsJsonAsync(
            $"/_matrix/client/v3/rooms/{RoomId}/redact/{eventToRedact}/{Guid.NewGuid()}", data)).Content.ReadFromJsonAsync<EventIdResponse>())!;
    }

    public async Task InviteUser(string userId, string? reason = null) {
        await _httpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/invite", new UserIdAndReason(userId, reason));
    }
}
