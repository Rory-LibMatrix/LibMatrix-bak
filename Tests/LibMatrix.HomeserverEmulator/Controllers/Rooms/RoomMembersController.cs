using LibMatrix.EventTypes.Spec.State;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Validations.Rules;

namespace LibMatrix.HomeserverEmulator.Controllers.Rooms;

[ApiController]
[Route("/_matrix/client/{version}/rooms/{roomId}/")]
public class RoomMembersController(ILogger<RoomMembersController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore) : ControllerBase {
    [HttpGet("members")]
    public async Task<List<StateEventResponse>> CreateRoom(string roomId, string? at = null, string? membership = null, string? not_membership = null) {
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
        
        var members = room.State.Where(x => x.Type == "m.room.member").ToList();
        
        if(membership != null)
            members = members.Where(x => (x.TypedContent as RoomMemberEventContent)?.Membership == membership).ToList();
        
        if(not_membership != null)
            members = members.Where(x => (x.TypedContent as RoomMemberEventContent)?.Membership != not_membership).ToList();

        if (at != null) {
            var evt = room.Timeline.FirstOrDefault(x => x.EventId == at);
            if (evt == null)
                throw new MatrixException() {
                    ErrorCode = "M_NOT_FOUND",
                    Error = "Event not found"
                };
            
            members = members.Where(x => x.OriginServerTs <= evt.OriginServerTs).ToList();
        }

        return members;
    }
}