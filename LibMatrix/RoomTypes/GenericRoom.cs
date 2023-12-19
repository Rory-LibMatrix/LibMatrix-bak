using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Spec;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.EventTypes.Spec.State.RoomInfo;
using LibMatrix.Homeservers;
using LibMatrix.Services;

namespace LibMatrix.RoomTypes;

public class GenericRoom {
    internal readonly AuthenticatedHomeserverGeneric Homeserver;

    public GenericRoom(AuthenticatedHomeserverGeneric homeserver, string roomId) {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("Room ID cannot be null or whitespace", nameof(roomId));
        Homeserver = homeserver;
        RoomId = roomId;
        // if (GetType() != typeof(SpaceRoom))
        if (GetType() == typeof(GenericRoom)) {
            AsSpace = new SpaceRoom(homeserver, RoomId);
        }
    }

    public string RoomId { get; set; }

    public async IAsyncEnumerable<StateEventResponse?> GetFullStateAsync() {
        var result = Homeserver.ClientHttpClient.GetAsyncEnumerableFromJsonAsync<StateEventResponse>($"/_matrix/client/v3/rooms/{RoomId}/state");
        await foreach (var resp in result) {
            yield return resp;
        }
    }

    public async Task<List<StateEventResponse>> GetFullStateAsListAsync() {
        return await Homeserver.ClientHttpClient.GetFromJsonAsync<List<StateEventResponse>>($"/_matrix/client/v3/rooms/{RoomId}/state");
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
            var resp = await Homeserver.ClientHttpClient.GetFromJsonAsync<T>(url);
            return resp;
#endif
        }
        catch (MatrixException e) {
            // if (e is not { ErrorCodode: "M_NOT_FOUND" }) {
            throw;
            // }

            // Console.WriteLine(e);
            // return default;
        }
    }

    public async Task<T?> GetStateOrNullAsync<T>(string type, string stateKey = "") {
        try {
            return await GetStateAsync<T>(type, stateKey);
        }
        catch (MatrixException e) {
            if (e.ErrorCode == "M_NOT_FOUND") return default;
            throw;
        }
    }

    public async Task<MessagesResponse> GetMessagesAsync(string from = "", int? limit = null, string dir = "b", string filter = "") {
        var url = $"/_matrix/client/v3/rooms/{RoomId}/messages?dir={dir}";
        if (!string.IsNullOrWhiteSpace(from)) url += $"&from={from}";
        if (limit is not null) url += $"&limit={limit}";
        if (!string.IsNullOrWhiteSpace(filter)) url += $"&filter={filter}";
        var res = await Homeserver.ClientHttpClient.GetFromJsonAsync<MessagesResponse>(url);
        return res;
    }

    /// <summary>
    /// Same as <see cref="GetMessagesAsync"/>, except keeps fetching more responses until the beginning of the room is found, or the target message limit is reached
    /// </summary>
    public async IAsyncEnumerable<MessagesResponse> GetManyMessagesAsync(string from = "", int limit = 100, string dir = "b", string filter = "", bool includeState = true,
        bool fixForward = false) {
        if (dir == "f" && fixForward) {
            var concat = new List<MessagesResponse>();
            while (true) {
                var resp = await GetMessagesAsync(from, int.MaxValue, "b", filter);
                concat.Add(resp);
                if (!includeState)
                    resp.State.Clear();
                if (resp.End is null) break;
                from = resp.End;
            }

            concat.Reverse();
            foreach (var eventResponse in concat) {
                limit -= eventResponse.State.Count + eventResponse.Chunk.Count;
                while (limit < 0) {
                    if (eventResponse.State.Count > 0 && eventResponse.State.Max(x => x.OriginServerTs) > eventResponse.Chunk.Max(x => x.OriginServerTs))
                        eventResponse.State.Remove(eventResponse.State.MaxBy(x => x.OriginServerTs));
                    else
                        eventResponse.Chunk.Remove(eventResponse.Chunk.MaxBy(x => x.OriginServerTs));

                    limit++;
                }

                eventResponse.Chunk.Reverse();
                eventResponse.State.Reverse();
                yield return eventResponse;
                if (limit <= 0) yield break;
            }
        }
        else {
            while (limit > 0) {
                var resp = await GetMessagesAsync(from, limit, dir, filter);

                if (!includeState)
                    resp.State.Clear();

                limit -= resp.Chunk.Count + resp.State.Count;
                yield return resp;
                if (resp.End is null) {
                    Console.WriteLine("End is null");
                    yield break;
                }

                from = resp.End;
            }
        }

        Console.WriteLine("End of GetManyAsync");
    }

    public async Task<string?> GetNameAsync() => (await GetStateAsync<RoomNameEventContent>("m.room.name"))?.Name;

    public async Task<RoomIdResponse> JoinAsync(string[]? homeservers = null, string? reason = null, bool checkIfAlreadyMember = true) {
        if (checkIfAlreadyMember) {
            try {
                _ = await GetCreateEventAsync();
                return new RoomIdResponse {
                    RoomId = RoomId
                };
            }
            catch { } //ignore
        }

        var joinUrl = $"/_matrix/client/v3/join/{HttpUtility.UrlEncode(RoomId)}";
        Console.WriteLine($"Calling {joinUrl} with {homeservers?.Length ?? 0} via's...");
        if (homeservers == null || homeservers.Length == 0) homeservers = new[] { RoomId.Split(':')[1] };
        var fullJoinUrl = $"{joinUrl}?server_name=" + string.Join("&server_name=", homeservers);
        var res = await Homeserver.ClientHttpClient.PostAsJsonAsync(fullJoinUrl, new {
            reason
        });
        return await res.Content.ReadFromJsonAsync<RoomIdResponse>() ?? throw new Exception("Failed to join room?");
    }

    public async IAsyncEnumerable<StateEventResponse> GetMembersAsync(bool joinedOnly = true) {
        var sw = Stopwatch.StartNew();
        var res = await Homeserver.ClientHttpClient.GetAsync($"/_matrix/client/v3/rooms/{RoomId}/members");
        Console.WriteLine($"Members call responded in {sw.GetElapsedAndRestart()}");
        // var resText = await res.Content.ReadAsStringAsync();
        Console.WriteLine($"Members call response read in {sw.GetElapsedAndRestart()}");
        var result = await JsonSerializer.DeserializeAsync<ChunkedStateEventResponse>(await res.Content.ReadAsStreamAsync(), new JsonSerializerOptions() {
            TypeInfoResolver = ChunkedStateEventResponseSerializerContext.Default,
        });
        Console.WriteLine($"Members call deserialised in {sw.GetElapsedAndRestart()}");
        foreach (var resp in result.Chunk) {
            if (resp?.Type != "m.room.member") continue;
            if (joinedOnly && (resp.TypedContent as RoomMemberEventContent)?.Membership is not "join") continue;
            yield return resp;
        }

        Console.WriteLine($"Members call iterated in {sw.GetElapsedAndRestart()}");
    }

#region Utility shortcuts

    public async Task<EventIdResponse> SendMessageEventAsync(RoomMessageEventContent content) =>
        await SendTimelineEventAsync("m.room.message", content);

    public async Task<List<string>?> GetAliasesAsync() {
        var res = await GetStateAsync<RoomAliasEventContent>("m.room.aliases");
        return res.Aliases;
    }

    public async Task<RoomCanonicalAliasEventContent?> GetCanonicalAliasAsync() =>
        await GetStateAsync<RoomCanonicalAliasEventContent>("m.room.canonical_alias");

    public async Task<RoomTopicEventContent?> GetTopicAsync() =>
        await GetStateAsync<RoomTopicEventContent>("m.room.topic");

    public async Task<RoomAvatarEventContent?> GetAvatarUrlAsync() =>
        await GetStateAsync<RoomAvatarEventContent>("m.room.avatar");

    public async Task<RoomJoinRulesEventContent?> GetJoinRuleAsync() =>
        await GetStateAsync<RoomJoinRulesEventContent>("m.room.join_rules");

    public async Task<RoomHistoryVisibilityEventContent?> GetHistoryVisibilityAsync() =>
        await GetStateAsync<RoomHistoryVisibilityEventContent?>("m.room.history_visibility");

    public async Task<RoomGuestAccessEventContent?> GetGuestAccessAsync() =>
        await GetStateAsync<RoomGuestAccessEventContent>("m.room.guest_access");

    public async Task<RoomCreateEventContent?> GetCreateEventAsync() =>
        await GetStateAsync<RoomCreateEventContent>("m.room.create");

    public async Task<string?> GetRoomType() {
        var res = await GetStateAsync<RoomCreateEventContent>("m.room.create");
        return res.Type;
    }

    public async Task<RoomPowerLevelEventContent?> GetPowerLevelsAsync() =>
        await GetStateAsync<RoomPowerLevelEventContent>("m.room.power_levels");

    public async Task<string> GetNameOrFallbackAsync(int maxMemberNames = 2) {
        try {
            return await GetNameAsync();
        }
        catch {
            try {
                var members = GetMembersAsync();
                var memberList = new List<string>();
                int memberCount = 0;
                await foreach (var member in members)
                    memberList.Add((member.TypedContent is RoomMemberEventContent memberEvent ? memberEvent.DisplayName : "") ?? "");
                memberCount = memberList.Count;
                memberList.RemoveAll(string.IsNullOrWhiteSpace);
                memberList = memberList.OrderBy(x => x).ToList();
                if (memberList.Count > maxMemberNames)
                    return string.Join(", ", memberList.Take(maxMemberNames)) + " and " + (memberCount - maxMemberNames) + " others.";
                return string.Join(", ", memberList);
            }
            catch {
                return RoomId;
            }
        }
    }

    public async Task InviteUsersAsync(IEnumerable<string> users, string? reason = null, bool skipExisting = true) {
        var tasks = users.Select(x => InviteUserAsync(x, reason, skipExisting)).ToList();
        await Task.WhenAll(tasks);
    }

    public async Task<string?> GetResolvedRoomAvatarUrlAsync(bool useOriginHomeserver = false) {
        var avatar = await GetAvatarUrlAsync();
        if (avatar?.Url is null) return null;
        if (!avatar.Url.StartsWith("mxc://")) return avatar.Url;
        if (useOriginHomeserver)
            try {
                var hs = avatar.Url.Split('/', 3)[1];
                return await new HomeserverResolverService().ResolveMediaUri(hs, avatar.Url);
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
    
        return Homeserver.ResolveMediaUri(avatar.Url);
    }

#endregion

#region Simple calls

    public async Task ForgetAsync() =>
        await Homeserver.ClientHttpClient.PostAsync($"/_matrix/client/v3/rooms/{RoomId}/forget", null);

    public async Task LeaveAsync(string? reason = null) =>
        await Homeserver.ClientHttpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/leave", new {
            reason
        });

    public async Task KickAsync(string userId, string? reason = null) =>
        await Homeserver.ClientHttpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/kick",
            new UserIdAndReason { UserId = userId, Reason = reason });

    public async Task BanAsync(string userId, string? reason = null) =>
        await Homeserver.ClientHttpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/ban",
            new UserIdAndReason { UserId = userId, Reason = reason });

    public async Task UnbanAsync(string userId) =>
        await Homeserver.ClientHttpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/unban",
            new UserIdAndReason { UserId = userId });

    public async Task InviteUserAsync(string userId, string? reason = null, bool skipExisting = true) {
        if (skipExisting && await GetStateAsync<RoomMemberEventContent>("m.room.member", userId) is not null)
            return;
        await Homeserver.ClientHttpClient.PostAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/invite", new UserIdAndReason(userId, reason));
    }

#endregion

#region Events

    public async Task<EventIdResponse?> SendStateEventAsync(string eventType, object content) =>
        await (await Homeserver.ClientHttpClient.PutAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/state/{eventType}", content))
            .Content.ReadFromJsonAsync<EventIdResponse>();

    public async Task<EventIdResponse?> SendStateEventAsync(string eventType, string stateKey, object content) =>
        await (await Homeserver.ClientHttpClient.PutAsJsonAsync($"/_matrix/client/v3/rooms/{RoomId}/state/{eventType}/{stateKey}", content))
            .Content.ReadFromJsonAsync<EventIdResponse>();

    public async Task<EventIdResponse> SendTimelineEventAsync(string eventType, TimelineEventContent content) {
        var res = await Homeserver.ClientHttpClient.PutAsJsonAsync(
            $"/_matrix/client/v3/rooms/{RoomId}/send/{eventType}/" + Guid.NewGuid(), content, new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        return await res.Content.ReadFromJsonAsync<EventIdResponse>() ?? throw new Exception("Failed to send event");
    }

    public async Task<EventIdResponse?> SendFileAsync(string fileName, Stream fileStream, string messageType = "m.file", string contentType = "application/octet-stream") {
        var url = await Homeserver.UploadFile(fileName, fileStream);
        var content = new RoomMessageEventContent() {
            MessageType = messageType,
            Url = url,
            Body = fileName,
            FileName = fileName,
            FileInfo = new() {
                Size = fileStream.Length,
                MimeType = contentType
            }
        };
        return await SendTimelineEventAsync("m.room.message", content);
    }

    public async Task<T?> GetRoomAccountDataAsync<T>(string key) {
        var res = await Homeserver.ClientHttpClient.GetAsync($"/_matrix/client/v3/user/{Homeserver.UserId}/rooms/{RoomId}/account_data/{key}");
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to get room account data: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to get room account data: {await res.Content.ReadAsStringAsync()}");
        }

        return await res.Content.ReadFromJsonAsync<T>();
    }

    public async Task SetRoomAccountDataAsync(string key, object data) {
        var res = await Homeserver.ClientHttpClient.PutAsJsonAsync($"/_matrix/client/v3/user/{Homeserver.UserId}/rooms/{RoomId}/account_data/{key}", data);
        if (!res.IsSuccessStatusCode) {
            Console.WriteLine($"Failed to set room account data: {await res.Content.ReadAsStringAsync()}");
            throw new InvalidDataException($"Failed to set room account data: {await res.Content.ReadAsStringAsync()}");
        }
    }

    public async Task<T> GetEventAsync<T>(string eventId) {
        return await Homeserver.ClientHttpClient.GetFromJsonAsync<T>($"/_matrix/client/v3/rooms/{RoomId}/event/{eventId}");
    }

    public async Task<EventIdResponse> RedactEventAsync(string eventToRedact, string reason) {
        var data = new { reason };
        return (await (await Homeserver.ClientHttpClient.PutAsJsonAsync(
            $"/_matrix/client/v3/rooms/{RoomId}/redact/{eventToRedact}/{Guid.NewGuid()}", data)).Content.ReadFromJsonAsync<EventIdResponse>())!;
    }

#endregion

#region Utilities

    public async Task<Dictionary<string, List<string>>> GetMembersByHomeserverAsync(bool joinedOnly = true) {
        if (Homeserver is AuthenticatedHomeserverMxApiExtended mxaeHomeserver)
            return await mxaeHomeserver.ClientHttpClient.GetFromJsonAsync<Dictionary<string, List<string>>>(
                $"/_matrix/client/v3/rooms/{RoomId}/members_by_homeserver?joined_only={joinedOnly}");
        Dictionary<string, List<string>> roomHomeservers = new();
        var members = GetMembersAsync();
        await foreach (var member in members) {
            string memberHs = member.StateKey.Split(':', 2)[1];
            roomHomeservers.TryAdd(memberHs, new());
            roomHomeservers[memberHs].Add(member.StateKey);
        }

        Console.WriteLine($"Finished processing {RoomId}");
        return roomHomeservers;
    }

#region Disband room

    public async Task DisbandRoomAsync() {
        var states = GetFullStateAsync();
        List<string> stateTypeIgnore = new() {
            "m.room.create",
            "m.room.power_levels",
            "m.room.join_rules",
            "m.room.history_visibility",
            "m.room.guest_access",
            "m.room.member",
        };
        await foreach (var state in states) {
            if (state is null || state.RawContent is not { Count: > 0 }) continue;
            if (state.Type == "m.room.member" && state.StateKey != Homeserver.UserId)
                try {
                    await BanAsync(state.StateKey, "Disbanding room");
                }
                catch (MatrixException e) {
                    if (e.ErrorCode != "M_FORBIDDEN") throw;
                }

            if (stateTypeIgnore.Contains(state.Type)) continue;
            await SendStateEventAsync(state.Type, state.StateKey, new());
        }
    }

#endregion

#endregion

    public readonly SpaceRoom AsSpace;
}

public class RoomIdResponse {
    [JsonPropertyName("room_id")]
    public string RoomId { get; set; } = null!;
}