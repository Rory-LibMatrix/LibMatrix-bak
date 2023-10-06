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
        var createRoomTasks = Enumerable.Range(0, 10).Select(_ => RoomAbstraction.GetTestRoom(hs)).ToList();
        await Task.WhenAll(createRoomTasks);

        var rooms = await hs.GetJoinedRooms();
        Assert.NotNull(rooms);
        Assert.NotEmpty(rooms);
        Assert.All(rooms, Assert.NotNull);
        Assert.True(rooms.Count >= 10, "Not enough rooms were found");

        await hs.Logout();
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
        var hs = await HomeserverAbstraction.GetHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        var otherUsers = HomeserverAbstraction.GetRandomHomeservers(15);
        Assert.NotNull(room);

        // var expectedCount = 1;

        var tasks = new List<Task>();
        await foreach(var otherUser in otherUsers) {
            tasks.Add(Task.Run(async () => {
                await room.InviteUserAsync(otherUser.UserId);
                await otherUser.GetRoom(room.RoomId).JoinAsync();
            }));
        }
        await Task.WhenAll(tasks);

        var states = room.GetMembersAsync(false);
        var count = 0;
        await foreach(var state in states) {
            count++;
        }
        // Assert.Equal(++expectedCount, count);
        Assert.Equal(16, count);
    }
}
