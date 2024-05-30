using LibMatrix.Tests.Abstractions;
using LibMatrix.Tests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Tests.BasicRoomEventTests;

public class RoomAvatarTests : TestBed<TestFixture> {
    private readonly HomeserverAbstraction _hsAbstraction;

    public RoomAvatarTests(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
        _hsAbstraction = _fixture.GetService<HomeserverAbstraction>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverAbstraction)}");
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetAvatarUrlAsync() {
        var hs = await _hsAbstraction.GetConfiguredHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var url = await room.GetAvatarUrlAsync();
        Assert.NotNull(url);
        Assert.NotNull(url.Url);
        Assert.NotEmpty(url.Url);

        await room.LeaveAsync();
    }
}