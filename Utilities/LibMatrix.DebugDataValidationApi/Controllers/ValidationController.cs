using System.Text.Json;
using LibMatrix.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.DebugDataValidationApi.Controllers;

[ApiController]
[Route("/")]
public class ValidationController(ILogger<ValidationController> logger) : ControllerBase {
    [HttpPost("/validate/{type}")]
    public Task<bool> Get([FromRoute] string type, [FromBody] JsonElement content) {
        var t = Type.GetType(type);
        if (t is null) {
            logger.LogWarning($"Type `{type}` does not exist!");
            throw new ArgumentException($"Unknown type {type}!");
        }

        logger.LogInformation($"Validating {type}...");
        return Task.FromResult(content.FindExtraJsonElementFields(t, "$"));
    }
}