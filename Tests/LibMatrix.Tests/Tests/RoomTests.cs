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
        return await HomeserverAbstraction.GetHomeserver();
    }

    [Fact]
    public async Task GetJoinedRoomsAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        //make 100 rooms
        var createRoomTasks = Enumerable.Range(0, 100).Select(_ => RoomAbstraction.GetTestRoom(hs)).ToList();
        await Task.WhenAll(createRoomTasks);

        var rooms = await hs.GetJoinedRooms();
        Assert.NotNull(rooms);
        Assert.NotEmpty(rooms);
        Assert.All(rooms, Assert.NotNull);
        Assert.Equal(100, rooms.Count);

        await hs.Logout();
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

    [Fact]
    public async Task GetMembersAsync() {
        Assert.True(StateEvent.KnownStateEventTypes is { Count: > 0 }, "StateEvent.KnownStateEventTypes is empty!");
        Assert.True(StateEvent.KnownStateEventTypesByName is { Count: > 0 }, "StateEvent.KnownStateEventTypesByName is empty!");

        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
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

    [Fact]
    public async Task JoinAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var id = await room.JoinAsync();
        Assert.NotNull(id);
        Assert.NotNull(id.RoomId);
        Assert.NotEmpty(id.RoomId);
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

    [Fact]
    public async Task ForgetAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        await room.ForgetAsync();
    }

    [Fact]
    public async Task LeaveAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        await room.LeaveAsync();
    }

    [Fact]
    public async Task KickAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var hs2 = await HomeserverAbstraction.GetRandomHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        await room.InviteUserAsync(hs2.UserId,"Unit test!");
        await hs2.GetRoom(room.RoomId).JoinAsync();
        await room.KickAsync(hs2.UserId, "test");
        var banState = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", hs2.UserId);
        Assert.NotNull(banState);
        Assert.Equal("leave", banState.Membership);
    }

    [Fact]
    public async Task BanAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var hs2 = await HomeserverAbstraction.GetRandomHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        await room.BanAsync(hs2.UserId, "test");
        var banState = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", hs2.UserId);
        Assert.NotNull(banState);
        Assert.Equal("ban", banState.Membership);
    }

    [Fact]
    public async Task UnbanAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var hs2 = await HomeserverAbstraction.GetRandomHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        await room.BanAsync(hs2.UserId, "test");
        var banState = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", hs2.UserId);
        Assert.NotNull(banState);
        Assert.Equal("ban", banState.Membership);
        await room.UnbanAsync(hs2.UserId);
        var unbanState = await room.GetStateAsync<RoomMemberEventContent>("m.room.member", hs2.UserId);
        Assert.NotNull(unbanState);
        Assert.Equal("leave", unbanState.Membership);
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task SendStateEventAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
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
    public async Task SendAndGetStateEventAsync() {
        await SendStateEventAsync();
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);

        await room.SendStateEventAsync("gay.rory.libmatrix.unit_tests", new ProfileResponseEventContent() {
            DisplayName = "wee_woo",
            AvatarUrl = "no"
        });
        await room.SendStateEventAsync("gay.rory.libmatrix.unit_tests", "state_key_maybe", new ProfileResponseEventContent() {
            DisplayName = "wee_woo",
            AvatarUrl = "yes"
        });

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

    [Fact]
    public async Task DisbandAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);

        await room.DisbandRoomAsync();
    }

    [Fact]
    public async Task SendFileAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);

        var res = await room.SendFileAsync("test.txt", new MemoryStream(Encoding.UTF8.GetBytes("This test was written by Emma [it/its], member of the Rory& system." +
                                                                                                            "\nIf you are reading this on matrix, it means the unit test for uploading a file works!")));
        Assert.NotNull(res);
        Assert.NotNull(res.EventId);
    }

    [Fact]
    public async Task GetSpaceChildrenAsync() {
        var hs = await HomeserverAbstraction.GetHomeserver();
        var space = await RoomAbstraction.GetTestSpace(hs, 2, false, 1);
        Assert.NotNull(space);
        var children = space.GetChildrenAsync();
        Assert.NotNull(children);
        int found = 0;
        await foreach (var room in children) {
            found++;
        }
        Assert.Equal(2, found);
    }

    [Fact]
    public async Task InviteAndJoinAsync() {
        var otherUsers = HomeserverAbstraction.GetRandomHomeservers(7);
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);

        // var expectedCount = 1;

        await foreach(var otherUser in otherUsers) {
            await room.InviteUserAsync(otherUser.UserId);
            await otherUser.GetRoom(room.RoomId).JoinAsync();
        }

        var states = room.GetMembersAsync(false);
        var count = 0;
        await foreach(var state in states) {
            count++;
        }
        // Assert.Equal(++expectedCount, count);
        Assert.Equal(8, count);
    }
}
