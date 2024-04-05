using System.Text.Json.Nodes;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Responses;
using LibMatrix.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/client/{version}/")]
public class AuthController(ILogger<AuthController> logger, UserStore userStore, TokenService tokenService) : ControllerBase {
    [HttpPost("login")]
    public async Task<LoginResponse> Login(LoginRequest request) {
        if (!request.Identifier.User.StartsWith('@'))
            request.Identifier.User = $"@{request.Identifier.User}:{tokenService.GenerateServerName(HttpContext)}";
        if (request.Identifier.User.EndsWith("localhost"))
            request.Identifier.User = request.Identifier.User.Replace("localhost", tokenService.GenerateServerName(HttpContext));

        var user = await userStore.GetUserById(request.Identifier.User);
        if (user is null) {
            user = await userStore.CreateUser(request.Identifier.User);
        }

        return user.Login();
    }

    [HttpGet("login")]
    public async Task<LoginFlowsResponse> GetLoginFlows() {
        return new LoginFlowsResponse {
            Flows = ((string[]) [
                "m.login.password",
                "m.login.recaptcha",
                "m.login.sso",
                "m.login.email.identity",
                "m.login.msisdn",
                "m.login.dummy",
                "m.login.registration_token",
            ]).Select(x => new LoginFlowsResponse.LoginFlow { Type = x }).ToList()
        };
    }

    [HttpPost("logout")]
    public async Task<object> Logout() {
        var token = tokenService.GetAccessToken(HttpContext);
        var user = await userStore.GetUserByToken(token);
        if (user == null)
            throw new MatrixException() {
                ErrorCode = "M_UNKNOWN_TOKEN",
                Error = "No such user"
            };

        if (!user.AccessTokens.ContainsKey(token))
            throw new MatrixException() {
                ErrorCode = MatrixException.ErrorCodes.M_NOT_FOUND,
                Error = "Token not found"
            };

        user.AccessTokens.Remove(token);
        return new { };
    }
}

public class LoginFlowsResponse {
    public required List<LoginFlow> Flows { get; set; }

    public class LoginFlow {
        public required string Type { get; set; }
    }
}