using System.Collections.Frozen;
using LibMatrix.HomeserverEmulator.Extensions;
using LibMatrix.HomeserverEmulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers.Rooms;

[ApiController]
[Route("/_matrix/client/{version}/rooms/{roomId}/state")]
public class RoomStateController(ILogger<RoomStateController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore) : ControllerBase {
    [HttpGet("")]
    public async Task<FrozenSet<StateEventResponse>> GetState(string roomId) {
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

        return room.State;
    }
    
    [HttpGet("{eventType}")]
    public async Task<StateEventResponse> GetState(string roomId, string eventType) {
        return await GetState(roomId, eventType, "");
    }
    
    [HttpGet("{eventType}/{stateKey}")]
    public async Task<StateEventResponse> GetState(string roomId, string eventType, string stateKey) {
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

        var stateEvent = room.State.FirstOrDefault(x => x.Type == eventType && x.StateKey == stateKey);
        if (stateEvent == null)
            throw new MatrixException() {
                ErrorCode = "M_NOT_FOUND",
                Error = "Event not found"
            };
        return stateEvent;
    }
        
    [HttpPut("{eventType}")]
    public async Task<EventIdResponse> SetState(string roomId, string eventType, [FromBody] StateEvent request) {
        return await SetState(roomId, eventType, "", request);
    }
    
    [HttpPut("{eventType}/{stateKey}")]
    public async Task<EventIdResponse> SetState(string roomId, string eventType, string stateKey, [FromBody] StateEvent request) {
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
        var evt = room.SetStateInternal(request.ToStateEvent(user, room));
        evt.Type = eventType;
        evt.StateKey = stateKey;
        return new EventIdResponse(evt);
    }
}