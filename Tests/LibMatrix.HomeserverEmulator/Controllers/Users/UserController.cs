using System.Text.Json.Nodes;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Filters;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/client/{version}/")]
public class UserController(ILogger<UserController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore) : ControllerBase {
    [HttpGet("account/whoami")]
    public async Task<WhoAmIResponse> Login() {
        var token = tokenService.GetAccessToken(HttpContext);
        if (token is null)
            throw new MatrixException() {
                ErrorCode = "M_UNAUTHORIZED",
                Error = "No token passed."
            };

        var user = await userStore.GetUserByToken(token, Random.Shared.Next(101) <= 10, tokenService.GenerateServerName(HttpContext));
        if (user is null)
            throw new MatrixException() {
                ErrorCode = "M_UNKNOWN_TOKEN",
                Error = "Invalid token."
            };
        var whoAmIResponse = new WhoAmIResponse {
            UserId = user.UserId
        };
        return whoAmIResponse;
    }

    [HttpGet("joined_rooms")]
    public async Task<object> GetJoinedRooms() {
        var token = tokenService.GetAccessToken(HttpContext);
        if (token is null)
            throw new MatrixException() {
                ErrorCode = "M_UNAUTHORIZED",
                Error = "No token passed."
            };

        var user = await userStore.GetUserByToken(token, false);
        if (user is null)
            throw new MatrixException() {
                ErrorCode = "M_UNAUTHORIZED",
                Error = "Invalid token."
            };
        // return user.JoinedRooms;

        return new {
            joined_rooms = roomStore._rooms.Where(r =>
                r.State.Any(s => s.StateKey == user.UserId && s.Type == RoomMemberEventContent.EventId && (s.TypedContent as RoomMemberEventContent).Membership == "join")
            ).Select(r => r.RoomId).ToList()
        };
    }
}