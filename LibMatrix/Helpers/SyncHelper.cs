using System.Diagnostics;
using System.Net.Http.Json;
using ArcaneLibs.Extensions;
using LibMatrix.Filters;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using Microsoft.Extensions.Logging;

namespace LibMatrix.Helpers;

public class SyncHelper(AuthenticatedHomeserverGeneric homeserver, ILogger? logger = null) {
    public string? Since { get; set; }
    public int Timeout { get; set; } = 30000;
    public string? SetPresence { get; set; } = "online";
    public SyncFilter? Filter { get; set; }
    public bool FullState { get; set; }

    public bool IsInitialSync { get; set; } = true;

    public TimeSpan MinimumDelay { get; set; } = new(0);

    public async Task<SyncResponse?> SyncAsync(CancellationToken? cancellationToken = null) {
        if (homeserver is null) {
            Console.WriteLine("Null passed as homeserver for SyncHelper!");
            throw new ArgumentNullException(nameof(homeserver), "Null passed as homeserver for SyncHelper!");
        }
        if (homeserver.ClientHttpClient is null) {
            Console.WriteLine("Homeserver for SyncHelper is not properly configured!");
            throw new ArgumentNullException(nameof(homeserver.ClientHttpClient), "Null passed as homeserver for SyncHelper!");
        }


        var sw = Stopwatch.StartNew();

        var url = $"/_matrix/client/v3/sync?timeout={Timeout}&set_presence={SetPresence}&full_state={(FullState ? "true" : "false")}";
        if (!string.IsNullOrWhiteSpace(Since)) url += $"&since={Since}";
        if (Filter is not null) url += $"&filter={Filter.ToJson(ignoreNull: true, indent: false)}";
        // Console.WriteLine("Calling: " + url);
        logger?.LogInformation("SyncHelper: Calling: {}", url);
        try {
            var httpResp = await homeserver.ClientHttpClient.GetAsync(url, cancellationToken: cancellationToken ?? CancellationToken.None);
            if (httpResp is null) throw new NullReferenceException("Failed to send HTTP request");
            logger?.LogInformation("Got sync response: {} bytes, {} elapsed", httpResp.Content.Headers.ContentLength ?? -1, sw.Elapsed);
            var deserializeSw = Stopwatch.StartNew();
            var resp = await httpResp.Content.ReadFromJsonAsync<SyncResponse>(cancellationToken: cancellationToken ?? CancellationToken.None);
            logger?.LogInformation("Deserialized sync response: {} bytes, {} elapsed, {} total", httpResp.Content.Headers.ContentLength ?? -1, deserializeSw.Elapsed, sw.Elapsed);
            var timeToWait = MinimumDelay.Subtract(sw.Elapsed);
            if (timeToWait.TotalMilliseconds > 0)
                await Task.Delay(timeToWait);
            return resp;
        }
        catch (TaskCanceledException) {
            Console.WriteLine("Sync cancelled!");
            logger?.LogWarning("Sync cancelled due to TaskCanceledException!");
        }
        catch (Exception e) {
            Console.WriteLine(e);
            logger?.LogError(e, "Failed to sync!\n{}", e.ToString());
        }

        return null;
    }

    public async IAsyncEnumerable<SyncResponse> EnumerateSyncAsync(CancellationToken? cancellationToken = null) {
        while (!cancellationToken?.IsCancellationRequested ?? true) {
            var sync = await SyncAsync(cancellationToken);
            if (sync is null) continue;
            if (!string.IsNullOrWhiteSpace(sync.NextBatch)) Since = sync.NextBatch;
            yield return sync;
        }
    }

    public async Task RunSyncLoopAsync(bool skipInitialSyncEvents = true, CancellationToken? cancellationToken = null) {
        var sw = Stopwatch.StartNew();
        int emptyInitialSyncCount = 0;
        var oldTimeout = Timeout;
        Timeout = 0;
        await foreach (var sync in EnumerateSyncAsync(cancellationToken)) {
            if (sync.ToJson(ignoreNull: true, indent: false).Length < 250) {
                emptyInitialSyncCount++;
                if (emptyInitialSyncCount > 5) {
                    IsInitialSync = false;
                    Timeout = oldTimeout;
                }
            }

            await RunSyncLoopCallbacksAsync(sync, IsInitialSync && skipInitialSyncEvents);
        }
    }

    private async Task RunSyncLoopCallbacksAsync(SyncResponse syncResponse, bool isInitialSync) {
        var tasks = SyncReceivedHandlers.Select(x => x(syncResponse)).ToList();
        await Task.WhenAll(tasks);

        if (syncResponse.AccountData is { Events.Count: > 0 }) {
            foreach (var accountDataEvent in syncResponse.AccountData.Events) {
                tasks = AccountDataReceivedHandlers.Select(x => x(accountDataEvent)).ToList();
                await Task.WhenAll(tasks);
            }
        }

        await RunSyncLoopRoomCallbacksAsync(syncResponse, isInitialSync);
    }

    private async Task RunSyncLoopRoomCallbacksAsync(SyncResponse syncResponse, bool isInitialSync) {
        if (syncResponse.Rooms is { Invite.Count: > 0 }) {
            foreach (var roomInvite in syncResponse.Rooms.Invite) {
                var tasks = InviteReceivedHandlers.Select(x => x(roomInvite)).ToList();
                await Task.WhenAll(tasks);
            }
        }

        if (isInitialSync) return;

        if (syncResponse.Rooms is { Join.Count: > 0 }) {
            foreach (var updatedRoom in syncResponse.Rooms.Join) {
                if (updatedRoom.Value.Timeline is null) continue;
                foreach (var stateEventResponse in updatedRoom.Value.Timeline.Events) {
                    stateEventResponse.RoomId = updatedRoom.Key;
                    var tasks = TimelineEventHandlers.Select(x => x(stateEventResponse)).ToList();
                    await Task.WhenAll(tasks);
                }
            }
        }
    }

    /// <summary>
    /// Event fired when a sync response is received
    /// </summary>
    public List<Func<SyncResponse, Task>> SyncReceivedHandlers { get; } = new();

    /// <summary>
    /// Event fired when a room invite is received
    /// </summary>
    public List<Func<KeyValuePair<string, SyncResponse.RoomsDataStructure.InvitedRoomDataStructure>, Task>> InviteReceivedHandlers { get; } = new();

    /// <summary>
    /// Event fired when a timeline event is received
    /// </summary>
    public List<Func<StateEventResponse, Task>> TimelineEventHandlers { get; } = new();

    /// <summary>
    /// Event fired when an account data event is received
    /// </summary>
    public List<Func<StateEventResponse, Task>> AccountDataReceivedHandlers { get; } = new();
}