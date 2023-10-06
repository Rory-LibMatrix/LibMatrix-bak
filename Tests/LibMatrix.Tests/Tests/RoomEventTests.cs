using System.Text;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Homeservers;
using LibMatrix.Services;
using LibMatrix.Tests.Abstractions;
using LibMatrix.Tests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Tests;

public class RoomEventTests : TestBed<TestFixture> {
    private readonly TestFixture _fixture;
    private readonly HomeserverResolverService _resolver;
    private readonly Config _config;
    private readonly HomeserverProviderService _provider;

    public RoomEventTests(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
        _fixture = fixture;
        _resolver = _fixture.GetService<HomeserverResolverService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverResolverService)}");
        _config = _fixture.GetService<Config>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(Config)}");
        _provider = _fixture.GetService<HomeserverProviderService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverProviderService)}");
    }

    private async Task<AuthenticatedHomeserverGeneric> GetHomeserver() {
        return await HomeserverAbstraction.GetHomeserver();
    }

    [Fact]
    public async Task GetNameAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var name = await room.GetNameAsync();
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetTopicAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var topic = await room.GetTopicAsync();
        Assert.NotNull(topic);
        Assert.NotNull(topic.Topic);
        Assert.NotEmpty(topic.Topic);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetAliasesAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var aliases = await room.GetAliasesAsync();
        Assert.NotNull(aliases);
        Assert.NotEmpty(aliases);
        Assert.All(aliases, Assert.NotNull);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetCanonicalAliasAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var alias = await room.GetCanonicalAliasAsync();
        Assert.NotNull(alias);
        Assert.NotNull(alias.Alias);
        Assert.NotEmpty(alias.Alias);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetAvatarUrlAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var url = await room.GetAvatarUrlAsync();
        Assert.NotNull(url);
        Assert.NotNull(url.Url);
        Assert.NotEmpty(url.Url);
    }

    [Fact]
    public async Task GetJoinRuleAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var rule = await room.GetJoinRuleAsync();
        Assert.NotNull(rule);
        Assert.NotNull(rule.JoinRule);
        Assert.NotEmpty(rule.JoinRule);
    }

    [Fact]
    public async Task GetHistoryVisibilityAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var visibility = await room.GetHistoryVisibilityAsync();
        Assert.NotNull(visibility);
        Assert.NotNull(visibility.HistoryVisibility);
        Assert.NotEmpty(visibility.HistoryVisibility);
    }

    [Fact]
    public async Task GetGuestAccessAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        try {
            var access = await room.GetGuestAccessAsync();
            Assert.NotNull(access);
            Assert.NotNull(access.GuestAccess);
            Assert.NotEmpty(access.GuestAccess);
        }
        catch (Exception e) {
            if (e is not MatrixException exception) throw;
            Assert.Equal("M_NOT_FOUND", exception.ErrorCode);
        }
    }

    [Fact]
    public async Task GetCreateEventAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var create = await room.GetCreateEventAsync();
        Assert.NotNull(create);
        Assert.NotNull(create.Creator);
        Assert.NotEmpty(create.RoomVersion!);
    }

    [Fact]
    public async Task GetRoomType() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        await room.GetRoomType();
    }

    [Fact]
    public async Task GetPowerLevelsAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var power = await room.GetPowerLevelsAsync();
        Assert.NotNull(power);
        Assert.NotNull(power.Ban);
        Assert.NotNull(power.Kick);
        Assert.NotNull(power.Invite);
        Assert.NotNull(power.Redact);
        Assert.NotNull(power.StateDefault);
        Assert.NotNull(power.EventsDefault);
        Assert.NotNull(power.UsersDefault);
        Assert.NotNull(power.Users);
        // Assert.NotNull(power.Events);
    }

}
