using LibMatrix.HomeserverEmulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers.ThirdParty;

[ApiController]
[Route("/_matrix/client/{version}/thirdparty/")]
public class ThirdPartyController(ILogger<ThirdPartyController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore) : ControllerBase {
    [HttpGet("protocols")]
    public async Task<object> GetProtocols() {
        return new { };
    }

    [HttpGet("location")]
    public async Task<List<object>> GetLocations([FromQuery] string alias) {
        // TODO: implement
        return [];
    }

    [HttpGet("location/{protocol}")]
    public async Task<List<object>> GetLocation([FromRoute] string protocol) {
        // TODO: implement
        return [];
    }
}