using LibMatrix.Services;
using LibMatrix.Tests.Abstractions;
using LibMatrix.Tests.DataTests;
using LibMatrix.Tests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Tests;

public class AuthTests : TestBed<TestFixture> {
    private readonly Config _config;
    private readonly HomeserverProviderService _provider;
    private readonly HomeserverAbstraction _hsAbstraction;

    public AuthTests(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
        _config = _fixture.GetService<Config>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(Config)}");
        _provider = _fixture.GetService<HomeserverProviderService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverProviderService)}");
        _hsAbstraction = _fixture.GetService<HomeserverAbstraction>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverAbstraction)}");
    }
    
    [Fact]
    public async Task LoginWithPassword() {
        var credentials = await _hsAbstraction.GetKnownCredentials();
        
        var login = await _provider.Login(_config.TestHomeserver!, credentials.username, credentials.password);
        Assert.NotNull(login);
        Assert.NotNull(login.AccessToken);
    }

    [Fact]
    public async Task LoginWithToken() {
        var credentials = await _hsAbstraction.GetKnownCredentials();
        var hs = await _provider.GetAuthenticatedWithToken(_config.TestHomeserver!, credentials.token);
        Assert.NotNull(hs);
        Assert.NotNull(hs.WhoAmI);
        hs.WhoAmI.VerifyRequiredFields();
        Assert.NotNull(hs.UserId);
        Assert.NotNull(hs.AccessToken);
        await hs.Logout();
    }

    [Fact]
    public async Task RegisterAsync() {
        var rhs = await _provider.GetRemoteHomeserver(_config.TestHomeserver);
        var reg = await rhs.RegisterAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Unit tests!");
        Assert.NotNull(reg);
        Assert.NotNull(reg.AccessToken);
        Assert.NotNull(reg.DeviceId);
        Assert.NotNull(reg.UserId);
        var hs = await _provider.GetAuthenticatedWithToken(reg.Homeserver, reg.AccessToken);
        Assert.NotNull(hs);
        Assert.NotNull(hs.WhoAmI);
        hs.WhoAmI.VerifyRequiredFields();
        Assert.NotNull(hs.UserId);
        Assert.NotNull(hs.AccessToken);
        await hs.Logout();
    }
}