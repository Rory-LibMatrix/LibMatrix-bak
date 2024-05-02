using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.HomeserverEmulator.Services;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/")]
public class DirectoryController(ILogger<DirectoryController> logger, RoomStore roomStore) : ControllerBase {
    [HttpGet("client/v3/directory/room/{alias}")]
    public async Task<AliasResult> GetRoomAliasV3(string alias) {
        var match = roomStore._rooms.FirstOrDefault(x =>
            x.State.Any(y => y.Type == RoomCanonicalAliasEventContent.EventId && y.StateKey == "" && y.RawContent?["alias"]?.ToString() == alias));

        if (match == null)
            throw new MatrixException() {
                ErrorCode = "M_NOT_FOUND",
                Error = "Room not found"
            };

        var servers = match.State.Where(x => x.Type == RoomMemberEventContent.EventId && x.RawContent?["membership"]?.ToString() == "join")
            .Select(x => x.StateKey!.Split(':', 2)[1]).ToList();

        return new() {
            RoomId = match.RoomId,
            Servers = servers
        };
    }
}