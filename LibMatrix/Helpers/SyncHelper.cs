using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.Filters;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.Services;

namespace LibMatrix.Helpers;

public class SyncHelper(AuthenticatedHomeserverGeneric homeserver) {
    public async Task<SyncResult?> Sync(
        string? since = null,
        int? timeout = 30000,
        string? setPresence = "online",
        SyncFilter? filter = null,
        CancellationToken? cancellationToken = null) {
        var url = $"/_matrix/client/v3/sync?timeout={timeout}&set_presence={setPresence}";
        if (!string.IsNullOrWhiteSpace(since)) url += $"&since={since}";
        if (filter is not null) url += $"&filter={filter.ToJson(ignoreNull: true, indent: false)}";
        // else url += "&full_state=true";
        Console.WriteLine("Calling: " + url);
        try {
            var req = await homeserver._httpClient.GetAsync(url, cancellationToken: cancellationToken ?? CancellationToken.None);

#if DEBUG && false
            try {
                await homeserver._httpClient.PostAsync(
                    "http://localhost:5116/validate/" + typeof(SyncResult).AssemblyQualifiedName,
                    new StreamContent(await req.Content.ReadAsStreamAsync()));
            }

            catch (Exception e) {
                Console.WriteLine("[!!] Checking sync response failed: " + e);
            }
            var res = await req.Content.ReadFromJsonAsync<SyncResult>();
            return res;
#else
            return await req.Content.ReadFromJsonAsync<SyncResult>();
#endif
        }
        catch (TaskCanceledException) {
            Console.WriteLine("Sync cancelled!");
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }

        return null;
    }

    [SuppressMessage("ReSharper", "FunctionNeverReturns")]
    public async Task RunSyncLoop(
        bool skipInitialSyncEvents = true,
        string? since = null,
        int? timeout = 30000,
        string? setPresence = "online",
        SyncFilter? filter = null,
        CancellationToken? cancellationToken = null
    ) {
        // await Task.WhenAll((await storageService.CacheStorageProvider.GetAllKeysAsync())
        //     .Where(x => x.StartsWith("sync"))
        //     .ToList()
        //     .Select(x => storageService.CacheStorageProvider.DeleteObjectAsync(x)));
        var nextBatch = since;
        while (cancellationToken is null || !cancellationToken.Value.IsCancellationRequested) {
            var sync = await Sync(since: nextBatch, timeout: timeout, setPresence: setPresence, filter: filter,
                cancellationToken: cancellationToken);
            nextBatch = sync?.NextBatch ?? nextBatch;
            if (sync is null) continue;
            Console.WriteLine($"Got sync, next batch: {nextBatch}!");

            if (sync.Rooms is { Invite.Count: > 0 }) {
                foreach (var roomInvite in sync.Rooms.Invite) {
                    var tasks = InviteReceivedHandlers.Select(x => x(roomInvite)).ToList();
                    await Task.WhenAll(tasks);
                }
            }

            if (sync.AccountData is { Events: { Count: > 0 } }) {
                foreach (var accountDataEvent in sync.AccountData.Events) {
                    var tasks = AccountDataReceivedHandlers.Select(x => x(accountDataEvent)).ToList();
                    await Task.WhenAll(tasks);
                }
            }

            // Things that are skipped on the first sync
            if (skipInitialSyncEvents) {
                skipInitialSyncEvents = false;
                continue;
            }

            if (sync.Rooms is { Join.Count: > 0 }) {
                foreach (var updatedRoom in sync.Rooms.Join) {
                    if(updatedRoom.Value.Timeline is null) continue;
                    foreach (var stateEventResponse in updatedRoom.Value.Timeline.Events) {
                        stateEventResponse.RoomId = updatedRoom.Key;
                        var tasks = TimelineEventHandlers.Select(x => {
                            try {
                                return x(stateEventResponse);
                            }
                            catch (Exception e) {
                                Console.WriteLine(e);
                                return Task.CompletedTask;
                            }
                        }).ToList();
                        await Task.WhenAll(tasks);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Event fired when a room invite is received
    /// </summary>
    public List<Func<KeyValuePair<string, SyncResult.RoomsDataStructure.InvitedRoomDataStructure>, Task>>
        InviteReceivedHandlers { get; } = new();

    public List<Func<StateEventResponse, Task>> TimelineEventHandlers { get; } = new();
    public List<Func<StateEventResponse, Task>> AccountDataReceivedHandlers { get; } = new();
}

public class SyncResult {
    [JsonPropertyName("next_batch")]
    public string NextBatch { get; set; }

    [JsonPropertyName("account_data")]
    public EventList? AccountData { get; set; }

    [JsonPropertyName("presence")]
    public PresenceDataStructure? Presence { get; set; }

    [JsonPropertyName("device_one_time_keys_count")]
    public Dictionary<string, int> DeviceOneTimeKeysCount { get; set; }

    [JsonPropertyName("rooms")]
    public RoomsDataStructure? Rooms { get; set; }

    [JsonPropertyName("to_device")]
    public EventList? ToDevice { get; set; }

    [JsonPropertyName("device_lists")]
    public DeviceListsDataStructure? DeviceLists { get; set; }

    public class DeviceListsDataStructure {
        [JsonPropertyName("changed")]
        public List<string>? Changed { get; set; }

        [JsonPropertyName("left")]
        public List<string>? Left { get; set; }
    }

    // supporting classes
    public class PresenceDataStructure {
        [JsonPropertyName("events")]
        public List<StateEventResponse> Events { get; set; }
    }

    public class RoomsDataStructure {
        [JsonPropertyName("join")]
        public Dictionary<string, JoinedRoomDataStructure>? Join { get; set; }

        [JsonPropertyName("invite")]
        public Dictionary<string, InvitedRoomDataStructure>? Invite { get; set; }

        public class JoinedRoomDataStructure {
            [JsonPropertyName("timeline")]
            public TimelineDataStructure? Timeline { get; set; }

            [JsonPropertyName("state")]
            public EventList State { get; set; }

            [JsonPropertyName("account_data")]
            public EventList AccountData { get; set; }

            [JsonPropertyName("ephemeral")]
            public EventList Ephemeral { get; set; }

            [JsonPropertyName("unread_notifications")]
            public UnreadNotificationsDataStructure UnreadNotifications { get; set; }

            [JsonPropertyName("summary")]
            public SummaryDataStructure Summary { get; set; }

            public class TimelineDataStructure {
                [JsonPropertyName("events")]
                public List<StateEventResponse> Events { get; set; }

                [JsonPropertyName("prev_batch")]
                public string PrevBatch { get; set; }

                [JsonPropertyName("limited")]
                public bool Limited { get; set; }
            }

            public class UnreadNotificationsDataStructure {
                [JsonPropertyName("notification_count")]
                public int NotificationCount { get; set; }

                [JsonPropertyName("highlight_count")]
                public int HighlightCount { get; set; }
            }

            public class SummaryDataStructure {
                [JsonPropertyName("m.heroes")]
                public List<string> Heroes { get; set; }

                [JsonPropertyName("m.invited_member_count")]
                public int InvitedMemberCount { get; set; }

                [JsonPropertyName("m.joined_member_count")]
                public int JoinedMemberCount { get; set; }
            }
        }

        public class InvitedRoomDataStructure {
            [JsonPropertyName("invite_state")]
            public EventList InviteState { get; set; }
        }
    }
}

public class EventList {
    [JsonPropertyName("events")]
    public List<StateEventResponse> Events { get; set; }
}
