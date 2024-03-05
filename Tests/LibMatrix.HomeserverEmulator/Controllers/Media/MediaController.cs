using LibMatrix.HomeserverEmulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers.Media;

[ApiController]
[Route("/_matrix/media/{version}/")]
public class MediaController(ILogger<MediaController> logger, TokenService tokenService, UserStore userStore, RoomStore roomStore) : ControllerBase {
    [HttpPost("upload")]
    public async Task<object> UploadMedia([FromHeader(Name = "Content-Type")] string ContentType, [FromQuery] string filename, [FromBody] Stream file) {
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
        
        
        
        var mediaId = Guid.NewGuid().ToString();
        var media = new {
            content_uri = $"mxc://{tokenService.GenerateServerName(HttpContext)}/{mediaId}"
        };
        return media;
        
    }
}