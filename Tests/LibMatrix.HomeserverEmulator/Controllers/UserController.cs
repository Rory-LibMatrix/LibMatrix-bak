using System.Text.Json.Nodes;
using ArcaneLibs.Extensions;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/client/{version}/")]
public class UserController(ILogger<UserController> logger, TokenService tokenService, UserStore userStore) : ControllerBase {
    [HttpGet("account/whoami")]
    public async Task<WhoAmIResponse> Login() {
        var token = tokenService.GetAccessToken();
        if (token is null)
            throw new MatrixException() {
                ErrorCode = "M_UNAUTHORIZED",
                Error = "No token passed."
            };

        var user = await userStore.GetUserByToken(token, Random.Shared.Next(101) <= 10, tokenService.GenerateServerName());
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
    
    [HttpGet("profile/{userId}")]
    public async Task<Dictionary<string, object>> GetProfile(string userId) {
        var user = await userStore.GetUserById(userId, false);
        if (user is null)
            throw new MatrixException() {
                ErrorCode = "M_NOT_FOUND",
                Error = "User not found."
            };
        return user.Profile;
    }
    
    [HttpGet("profile/{userId}/{key}")]
    public async Task<object> GetProfile(string userId, string key) {
        var user = await userStore.GetUserById(userId, false);
        if (user is null)
            throw new MatrixException() {
                ErrorCode = "M_NOT_FOUND",
                Error = "User not found."
            };
        if (!user.Profile.TryGetValue(key, out var value))
            throw new MatrixException() {
                ErrorCode = "M_NOT_FOUND",
                Error = "Key not found."
            };
        return value;
    }
    
    [HttpGet("joined_rooms")]
    public async Task<object> GetJoinedRooms() {
        var token = tokenService.GetAccessToken();
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
            joined_rooms = user.JoinedRooms
        };
    }
}