namespace LibMatrix.Tests;

public class Config {
    public string? TestHomeserver { get; set; } = Environment.GetEnvironmentVariable("LIBMATRIX_TEST_HOMESERVER") ?? null;
    public string? TestUsername { get; set; } = Environment.GetEnvironmentVariable("LIBMATRIX_TEST_USERNAME") ?? null;
    public string? TestPassword { get; set; } = Environment.GetEnvironmentVariable("LIBMATRIX_TEST_PASSWORD") ?? null;
    public string? TestRoomId { get; set; } = Environment.GetEnvironmentVariable("LIBMATRIX_TEST_ROOM_ID") ?? null;
    public string? TestRoomAlias { get; set; } = Environment.GetEnvironmentVariable("LIBMATRIX_TEST_ROOM_ALIAS") ?? null;

    public Dictionary<string, string> ExpectedHomeserverMappings { get; set; } = new() {
        {"matrix.org", "https://matrix-client.matrix.org"},
        {"rory.gay", "https://matrix.rory.gay"}
    };
    public Dictionary<string, string> ExpectedAliasMappings { get; set; } = new() {
        {"#libmatrix:rory.gay", "!tuiLEoMqNOQezxILzt:rory.gay"}
    };
}
