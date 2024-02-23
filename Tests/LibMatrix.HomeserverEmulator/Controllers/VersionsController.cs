using System.Text.Json.Nodes;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.HomeserverEmulator.Controllers;

[ApiController]
[Route("/_matrix/")]
public class VersionsController(ILogger<WellKnownController> logger) : ControllerBase {
    [HttpGet("client/versions")]
    public async Task<ClientVersionsResponse> GetClientVersions() {
        var clientVersions = new ClientVersionsResponse {
            Versions = new() {
                "r0.0.1",
                "r0.1.0",
                "r0.2.0",
                "r0.3.0",
                "r0.4.0",
                "r0.5.0",
                "r0.6.0",
                "r0.6.1",
                "v1.1",
                "v1.2",
                "v1.3",
                "v1.4",
                "v1.5",
                "v1.6",
                "v1.7",
                "v1.8",
            },
            UnstableFeatures = new()
        };
        return clientVersions;
    }

    [HttpGet("federation/v1/version")]
    public async Task<ServerVersionResponse> GetServerVersions() {
        var clientVersions = new ServerVersionResponse() {
            Server = new() {
                Name = "LibMatrix.HomeserverEmulator",
                Version = "0.0.0"
            }
        };
        return clientVersions;
    }
}