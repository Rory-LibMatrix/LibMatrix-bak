using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Homeservers;
using LibMatrix.Services;
using LibMatrix.Tests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Tests;

public class RoomTests : TestBed<TestFixture> {
    private readonly TestFixture _fixture;
    private readonly HomeserverResolverService _resolver;
    private readonly Config _config;
    private readonly HomeserverProviderService _provider;

    public RoomTests(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
        _fixture = fixture;
        _resolver = _fixture.GetService<HomeserverResolverService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverResolverService)}");
        _config = _fixture.GetService<Config>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(Config)}");
        _provider = _fixture.GetService<HomeserverProviderService>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverProviderService)}");
    }

    private async Task<AuthenticatedHomeserverGeneric> GetHomeserver() {
        Assert.False(string.IsNullOrWhiteSpace(_config.TestHomeserver), $"{nameof(_config.TestHomeserver)} must be set in appsettings!");
        Assert.False(string.IsNullOrWhiteSpace(_config.TestUsername), $"{nameof(_config.TestUsername)} must be set in appsettings!");
        Assert.False(string.IsNullOrWhiteSpace(_config.TestPassword), $"{nameof(_config.TestPassword)} must be set in appsettings!");

        // var server = await _resolver.ResolveHomeserverFromWellKnown(_config.TestHomeserver!);
        var login = await _provider.Login(_config.TestHomeserver!, _config.TestUsername!, _config.TestPassword!);
        Assert.NotNull(login);

        var hs = await _provider.GetAuthenticatedWithToken(_config.TestHomeserver!, login.AccessToken);
        return hs;
    }

    [Fact]
    public async Task GetJoinedRoomsAsync() {
        var hs = await GetHomeserver();

        var rooms = await hs.GetJoinedRooms();
        Assert.NotNull(rooms);
        Assert.NotEmpty(rooms);
        Assert.All(rooms, Assert.NotNull);

        await hs.Logout();
    }

    [Fact]
    public async Task GetNameAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var name = await room.GetNameAsync();
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetTopicAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var topic = await room.GetTopicAsync();
        Assert.NotNull(topic);
        Assert.NotNull(topic.Topic);
        Assert.NotEmpty(topic.Topic);
    }

    [Fact]
    public async Task GetMembersAsync() {
        Assert.True(StateEvent.KnownStateEventTypes is { Count: > 0 }, "StateEvent.KnownStateEventTypes is empty!");
        Assert.True(StateEvent.KnownStateEventTypesByName is { Count: > 0 }, "StateEvent.KnownStateEventTypesByName is empty!");

        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var members = room.GetMembersAsync();
        Assert.NotNull(members);
        bool hitMembers = false;
        await foreach (var member in members) {
            Assert.NotNull(member);
            Assert.NotNull(member.StateKey);
            Assert.NotEmpty(member.StateKey);
            Assert.NotNull(member.Sender);
            Assert.NotEmpty(member.Sender);
            Assert.NotNull(member.RawContent);
            Assert.NotEmpty(member.RawContent);
            Assert.NotNull(member.TypedContent);
            Assert.IsType<RoomMemberEventContent>(member.TypedContent);
            var content = (RoomMemberEventContent)member.TypedContent;
            Assert.NotNull(content);
            Assert.NotNull(content.Membership);
            Assert.NotEmpty(content.Membership);
            hitMembers = true;
        }

        Assert.True(hitMembers, "No members were found in the room");
    }

    /*
     tests remaining:
     GetStateAsync(string,string) 0% 8/8
       GetMessagesAsync(string,int,string,string) 0% 7/7
       JoinAsync(string[],string) 0% 8/8
       SendMessageEventAsync(RoomMessageEventContent) 0% 1/1
       GetAliasesAsync() 0% 4/4
       GetCanonicalAliasAsync() 0% 1/1
       GetAvatarUrlAsync() 0% 1/1
       GetJoinRuleAsync() 0% 1/1
       GetHistoryVisibilityAsync() 0% 1/1
       GetGuestAccessAsync() 0% 1/1
       GetCreateEventAsync() 0% 1/1
       GetRoomType() 0% 4/4
       GetPowerLevelsAsync() 0% 1/1
       ForgetAsync() 0% 1/1
       LeaveAsync(string) 0% 1/1
       KickAsync(string,string) 0% 1/1
       BanAsync(string,string) 0% 1/1
       UnbanAsync(string) 0% 1/1
       SendStateEventAsync(string,object) 0% 1/1
       SendStateEventAsync(string,string,object) 0% 1/1
       SendTimelineEventAsync(string,EventContent) 0% 5/5
       SendFileAsync(string,string,Stream) 0% 6/6
       GetRoomAccountData<T>(string) 0% 8/8
       SetRoomAccountData(string,object) 0% 7/7
       GetEvent<T>(string) 0% 3/3
       RedactEventAsync(string,string) 0% 4/4
       InviteUser(string,string) 0% 3/3
     */

    [Fact]
    public async Task JoinAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var id = await room.JoinAsync();
        Assert.NotNull(id);
        Assert.NotNull(id.RoomId);
        Assert.NotEmpty(id.RoomId);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetAliasesAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var aliases = await room.GetAliasesAsync();
        Assert.NotNull(aliases);
        Assert.NotEmpty(aliases);
        Assert.All(aliases, Assert.NotNull);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetCanonicalAliasAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var alias = await room.GetCanonicalAliasAsync();
        Assert.NotNull(alias);
        Assert.NotNull(alias.Alias);
        Assert.NotEmpty(alias.Alias);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetAvatarUrlAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var url = await room.GetAvatarUrlAsync();
        Assert.NotNull(url);
        Assert.NotNull(url.Url);
        Assert.NotEmpty(url.Url);
    }

    [Fact]
    public async Task GetJoinRuleAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var rule = await room.GetJoinRuleAsync();
        Assert.NotNull(rule);
        Assert.NotNull(rule.JoinRule);
        Assert.NotEmpty(rule.JoinRule);
    }

    [Fact]
    public async Task GetHistoryVisibilityAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var visibility = await room.GetHistoryVisibilityAsync();
        Assert.NotNull(visibility);
        Assert.NotNull(visibility.HistoryVisibility);
        Assert.NotEmpty(visibility.HistoryVisibility);
    }

    [Fact]
    public async Task GetGuestAccessAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        try {
            var access = await room.GetGuestAccessAsync();
            Assert.NotNull(access);
            Assert.NotNull(access.GuestAccess);
            Assert.NotEmpty(access.GuestAccess);
        }
        catch (Exception e) {
            if(e is not MatrixException exception) throw;
            Assert.Equal("M_NOT_FOUND", exception.ErrorCode);
        }
    }

    [Fact]
    public async Task GetCreateEventAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var create = await room.GetCreateEventAsync();
        Assert.NotNull(create);
        Assert.NotNull(create.Creator);
        Assert.NotEmpty(create.RoomVersion!);
    }

    [Fact]
    public async Task GetRoomType() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        await room.GetRoomType();
    }

    [Fact]
    public async Task GetPowerLevelsAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
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
        Assert.NotNull(power.Events);
    }

    [Fact(Skip = "This test is destructive!")]
    public async Task ForgetAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        await room.ForgetAsync();
    }

    [Fact(Skip = "This test is destructive!")]
    public async Task LeaveAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        await room.LeaveAsync();
    }

    [Fact(Skip = "This test is destructive!")]
    public async Task KickAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        // await room.KickAsync(_config.TestUserId, "test");
    }

    [Fact(Skip = "This test is destructive!")]
    public async Task BanAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        // await room.BanAsync(_config.TestUserId, "test");
    }

    [Fact(Skip = "This test is destructive!")]
    public async Task UnbanAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        // await room.UnbanAsync(_config.TestUserId);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task SendStateEventAsync() {
        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        await room.SendStateEventAsync("gay.rory.libmatrix.unit_tests", new ProfileResponseEventContent() {
            DisplayName = "wee_woo",
            AvatarUrl = "no"
        });
        await room.SendStateEventAsync("gay.rory.libmatrix.unit_tests", "state_key_maybe", new ProfileResponseEventContent() {
            DisplayName = "wee_woo",
            AvatarUrl = "yes"
        });
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetStateEventAsync() {
        await SendStateEventAsync();

        var hs = await GetHomeserver();
        var room = hs.GetRoom(_config.TestRoomId);
        Assert.NotNull(room);
        var state1 = await room.GetStateAsync<ProfileResponseEventContent>("gay.rory.libmatrix.unit_tests");
        Assert.NotNull(state1);
        Assert.NotNull(state1.DisplayName);
        Assert.NotEmpty(state1.DisplayName);
        Assert.NotNull(state1.AvatarUrl);
        Assert.NotEmpty(state1.AvatarUrl);
        Assert.Equal("wee_woo", state1.DisplayName);
        Assert.Equal("no", state1.AvatarUrl);

        var state2 = await room.GetStateAsync<ProfileResponseEventContent>("gay.rory.libmatrix.unit_tests", "state_key_maybe");
        Assert.NotNull(state2);
        Assert.NotNull(state2.DisplayName);
        Assert.NotEmpty(state2.DisplayName);
        Assert.NotNull(state2.AvatarUrl);
        Assert.NotEmpty(state2.AvatarUrl);
        Assert.Equal("wee_woo", state2.DisplayName);
        Assert.Equal("yes", state2.AvatarUrl);
    }
}
