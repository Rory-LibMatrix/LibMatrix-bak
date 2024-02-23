using System.Text.Json.Nodes;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Responses;
using LibMatrix.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/client/{version}/")]
public class AuthController(ILogger<AuthController> logger, UserStore userStore) : ControllerBase {
    [HttpPost("login")]
    public async Task<LoginResponse> Login(LoginRequest request) {
        var user = await userStore.CreateUser($"@{Guid.NewGuid().ToString()}:{Request.Host}", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new Dictionary<string, object>());
        var loginResponse = new LoginResponse {
            AccessToken = user.AccessToken,
            DeviceId = user.DeviceId,
            UserId = user.UserId
        };

        return loginResponse;
    }
}