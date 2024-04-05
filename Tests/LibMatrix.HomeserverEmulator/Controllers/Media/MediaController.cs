using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ArcaneLibs.Extensions;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers.Media;

[ApiController]
[Route("/_matrix/media/{version}/")]
public class MediaController(
    ILogger<MediaController> logger,
    TokenService tokenService,
    UserStore userStore,
    HSEConfiguration cfg,
    HomeserverResolverService hsResolver,
    MediaStore mediaStore)
    : ControllerBase {
    [HttpPost("upload")]
    public async Task<object> UploadMedia([FromHeader(Name = "Content-Type")] string ContentType, [FromQuery] string filename, [FromBody] Stream file) {
        var token = tokenService.GetAccessTokenOrNull(HttpContext);
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

    private Dictionary<string, SemaphoreSlim> downloadLocks = new();

    [HttpGet("download/{serverName}/{mediaId}")]
    public async Task DownloadMedia(string serverName, string mediaId) {
        while (true)
            try {
                if (cfg.StoreData) {
                    SemaphoreSlim ss;
                    if (!downloadLocks.ContainsKey(serverName + mediaId))
                        downloadLocks[serverName + mediaId] = new SemaphoreSlim(1);
                    ss = downloadLocks[serverName + mediaId];
                    await ss.WaitAsync();
                    var serverMediaPath = Path.Combine(cfg.DataStoragePath, "media", serverName);
                    Directory.CreateDirectory(serverMediaPath);
                    var mediaPath = Path.Combine(serverMediaPath, mediaId);
                    if (System.IO.File.Exists(mediaPath)) {
                        ss.Release();
                        await using var stream = new FileStream(mediaPath, FileMode.Open);
                        await stream.CopyToAsync(Response.Body);
                        return;
                    }
                    else {
                        var mediaUrl = await hsResolver.ResolveMediaUri(serverName, $"mxc://{serverName}/{mediaId}");
                        if (mediaUrl is null)
                            throw new MatrixException() {
                                ErrorCode = "M_NOT_FOUND",
                                Error = "Media not found"
                            };
                        await using var stream = System.IO.File.OpenWrite(mediaPath);
                        using var response = await new HttpClient().GetAsync(mediaUrl);
                        await response.Content.CopyToAsync(stream);
                        await stream.FlushAsync();
                        ss.Release();
                        await DownloadMedia(serverName, mediaId);
                        return;
                    }
                }
                else {
                    var mediaUrl = await hsResolver.ResolveMediaUri(serverName, $"mxc://{serverName}/{mediaId}");
                    if (mediaUrl is null)
                        throw new MatrixException() {
                            ErrorCode = "M_NOT_FOUND",
                            Error = "Media not found"
                        };
                    using var response = await new HttpClient().GetAsync(mediaUrl);
                    await response.Content.CopyToAsync(Response.Body);
                    return;
                }

                return;
            }
            catch (IOException) {
                //ignored
            }
    }

    [HttpGet("thumbnail/{serverName}/{mediaId}")]
    public async Task DownloadThumbnail(string serverName, string mediaId) {
        await DownloadMedia(serverName, mediaId);
    }

    [HttpGet("preview_url")]
    public async Task<JsonObject> GetPreviewUrl([FromQuery] string url) {
        JsonObject data = new();

        using var hc = new HttpClient();
        using var response = await hc.GetAsync(url);
        var doc = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(doc, "<meta property=\"(.*?)\" content=\"(.*?)\"");

        while (match.Success) {
            data[match.Groups[1].Value] = match.Groups[2].Value;
            match = match.NextMatch();
        }

        return data;
    }
}