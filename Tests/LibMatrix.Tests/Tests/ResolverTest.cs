using LibMatrix.Services;
using LibMatrix.Tests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Tests;

public class ResolverTest : TestBed<TestFixture> {
    private readonly TestFixture _fixture;
    private readonly HomeserverResolverService _resolver;
    private readonly Config _config;
    private readonly HomeserverProviderService _provider;
    public ResolverTest(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
        _fixture = fixture;
        _resolver = _fixture.GetService<HomeserverResolverService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverResolverService)}");
        _config = _fixture.GetService<Config>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(Config)}");
        _provider = _fixture.GetService<HomeserverProviderService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverProviderService)}");
    }

    [Fact]
    public async Task ResolveServer() {
        foreach (var (domain, expected) in _config.ExpectedHomeserverMappings) {
            var server = await _resolver.ResolveHomeserverFromWellKnown(domain);
            Assert.Equal(expected, server);
        }
    }

    [Fact]
    public async Task ResolveMedia() {
        var media = await _resolver.ResolveMediaUri("matrix.org", "mxc://matrix.org/eqwrRZRoPpNbcMeUwyXAuVRo");
        Assert.Equal("https://matrix-client.matrix.org/_matrix/media/v3/download/matrix.org/eqwrRZRoPpNbcMeUwyXAuVRo", media);
    }

    [Fact]
    public async Task ResolveRoomAliasAsync() {
        var hs = await _provider.GetRemoteHomeserver("matrix.org");
        var alias = await hs.ResolveRoomAliasAsync("#matrix:matrix.org");
        Assert.Equal("!OGEhHVWSdvArJzumhm:matrix.org", alias.RoomId);
    }

    [Fact]
    public async Task GetClientVersionsAsync() {
        var hs = await _provider.GetRemoteHomeserver("matrix.org");
        var versions = await hs.GetClientVersionsAsync();
        Assert.NotNull(versions);
    }

    [Fact]
    public async Task GetProfileAsync() {
        var hs = await _provider.GetRemoteHomeserver("matrix.org");
        var profile = await hs.GetProfileAsync("@alice-is-:matrix.org");
        Assert.NotNull(profile);
    }
}
