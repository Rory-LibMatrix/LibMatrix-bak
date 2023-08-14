using System.Text.Json;
using LibMatrix.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LibMatrix.DebugDataValidationApi.Controllers;

[ApiController]
[Route("/")]
public class ValidationController : ControllerBase {
    private readonly ILogger<ValidationController> _logger;

    public ValidationController(ILogger<ValidationController> logger) {
        _logger = logger;
    }

    [HttpPost("/validate/{type}")]
    public async Task<bool> Get([FromRoute] string type, [FromBody] JsonElement content) {
        Type t = Type.GetType(type);
        if (t is null) {
            Console.WriteLine($"Type `{type}` does not exist!");
            throw new ArgumentException($"Unknown type {type}!");
        }
        Console.WriteLine($"Validating {type}...");
        return content.FindExtraJsonElementFields(t, "$");
    }
}
