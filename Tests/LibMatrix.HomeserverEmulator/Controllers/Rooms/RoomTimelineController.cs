using System.Collections.Frozen;
using System.Text.Json.Nodes;
using LibMatrix.HomeserverEmulator.Extensions;
using LibMatrix.HomeserverEmulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers.Rooms;

[ApiController]
[Route("/_matrix/client/{version}/rooms/{roomId}")]
public class RoomTimelineController(ILogger<RoomTimelineController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore) : ControllerBase {
    [HttpPut("send/{eventType}/{txnId}")]
    public async Task<EventIdResponse> SendMessage(string roomId, string eventType, string txnId, [FromBody] JsonObject content) {
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

        var room = roomStore.GetRoomById(roomId);
        if (room == null)
            throw new MatrixException() {
                ErrorCode = "M_NOT_FOUND",
                Error = "Room not found"
            };

        if (!room.JoinedMembers.Any(x=>x.StateKey == user.UserId))
            throw new MatrixException() {
                ErrorCode = "M_FORBIDDEN",
                Error = "User is not in the room"
            };

        var evt = new StateEvent() {
            RawContent = content,
            Type = eventType
        }.ToStateEvent(user, room);

        room.Timeline.Add(evt);

        return new() {
            EventId = evt.EventId
        };
    }
}