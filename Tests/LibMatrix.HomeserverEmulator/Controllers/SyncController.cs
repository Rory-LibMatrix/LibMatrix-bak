using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ArcaneLibs.Extensions;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/client/{version}/")]
public class SyncController(ILogger<SyncController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore, HSEConfiguration cfg) : ControllerBase {
    [HttpGet("sync")]
    [SuppressMessage("ReSharper.DPA", "DPA0011: High execution time of MVC action", Justification = "Endpoint is expected to wait until data is available or timeout.")]
    public async Task<SyncResponse> Sync([FromQuery] string? since = null, [FromQuery] int? timeout = 5000) {
        var sw = Stopwatch.StartNew();
        var token = tokenService.GetAccessToken(HttpContext);
        if (token == null)
            throw new MatrixException() {
                ErrorCode = "M_MISSING_TOKEN",
                Error = "Missing token"
            };

        var user = await userStore.GetUserByToken(token);
        if (user == null)
            throw new MatrixException() {
                ErrorCode = "M_UNKNOWN_TOKEN",
                Error = "No such user"
            };
        var session = user.AccessTokens[token];

        if (string.IsNullOrWhiteSpace(since))
            return InitialSync(user, session);

        if (!session.SyncStates.TryGetValue(since, out var syncState))
            if (!cfg.UnknownSyncTokenIsInitialSync)
                throw new MatrixException() {
                    ErrorCode = "M_UNKNOWN",
                    Error = "Unknown sync token."
                };
            else
                return InitialSync(user, session);

        var response = new SyncResponse() {
            NextBatch = Guid.NewGuid().ToString(),
            DeviceOneTimeKeysCount = new()
        };

        session.SyncStates.Add(response.NextBatch, new() {
            RoomPositions = syncState.RoomPositions.ToDictionary(x => x.Key, x => new UserStore.User.SessionInfo.UserSyncState.SyncRoomPosition() {
                TimelinePosition = roomStore._rooms.First(y => y.RoomId == x.Key).Timeline.Count,
                AccountDataPosition = roomStore._rooms.First(y => y.RoomId == x.Key).AccountData[user.UserId].Count
            })
        });

        if (!string.IsNullOrWhiteSpace(since)) {
            while (sw.ElapsedMilliseconds < timeout && response.Rooms?.Join is not { Count: > 0 }) {
                await Task.Delay(100);
                var rooms = roomStore._rooms.Where(x => x.State.Any(y => y.Type == "m.room.member" && y.StateKey == user.UserId)).ToList();
                foreach (var room in rooms) {
                    var roomPositions = syncState.RoomPositions[room.RoomId];

                    response.Rooms ??= new();
                    response.Rooms.Join ??= new();
                    response.Rooms.Join[room.RoomId] = new() {
                        Timeline = new(events: room.Timeline.Skip(roomPositions.TimelinePosition).ToList(), limited: false),
                        AccountData = new(room.AccountData.GetOrCreate(user.UserId, _ => []).Skip(roomPositions.AccountDataPosition).ToList())
                    };
                    if (response.Rooms.Join[room.RoomId].Timeline?.Events?.Count > 0)
                        response.Rooms.Join[room.RoomId].State = new(response.Rooms.Join[room.RoomId].Timeline!.Events.Where(x => x.StateKey != null).ToList());
                    session.SyncStates[response.NextBatch].RoomPositions[room.RoomId] = new() {
                        TimelinePosition = room.Timeline.Count,
                        AccountDataPosition = room.AccountData[user.UserId].Count
                    };

                    if (response.Rooms.Join[room.RoomId].State?.Events?.Count == 0 &&
                        response.Rooms.Join[room.RoomId].Timeline?.Events?.Count == 0 &&
                        response.Rooms.Join[room.RoomId].AccountData?.Events?.Count == 0
                       )
                        response.Rooms.Join.Remove(room.RoomId);
                }
            }
        }

        return response;
    }

    private SyncResponse InitialSync(UserStore.User user, UserStore.User.SessionInfo session) {
        var response = new SyncResponse() {
            NextBatch = Guid.NewGuid().ToString(),
            DeviceOneTimeKeysCount = new(),
            AccountData = new(events: user.AccountData.ToList())
        };

        session.SyncStates.Add(response.NextBatch, new());

        var rooms = roomStore._rooms.Where(x => x.State.Any(y => y.Type == "m.room.member" && y.StateKey == user.UserId)).ToList();
        foreach (var room in rooms) {
            response.Rooms ??= new();
            response.Rooms.Join ??= new();
            response.Rooms.Join[room.RoomId] = new() {
                State = new(room.State.ToList()),
                Timeline = new(events: room.Timeline.ToList(), limited: false),
                AccountData = new(room.AccountData.GetOrCreate(user.UserId, _ => []).ToList())
            };
            session.SyncStates[response.NextBatch].RoomPositions[room.RoomId] = new() {
                TimelinePosition = room.Timeline.Count,
                AccountDataPosition = room.AccountData[user.UserId].Count
            };
        }

        return response;
    }
}