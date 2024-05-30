using LibMatrix.Tests.Abstractions;
using LibMatrix.Tests.Fixtures;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace LibMatrix.Tests.Tests.BasicRoomEventTests;

public class RoomTopicTests : TestBed<TestFixture> {
    private readonly HomeserverAbstraction _hsAbstraction;

    public RoomTopicTests(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
        _hsAbstraction = _fixture.GetService<HomeserverAbstraction>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverAbstraction)}");
    }

    [SkippableFact(typeof(MatrixException))]
    public async Task GetTopicAsync() {
        var hs = await _hsAbstraction.GetConfiguredHomeserver();
        var room = await RoomAbstraction.GetTestRoom(hs);
        Assert.NotNull(room);
        var topic = await room.GetTopicAsync();
        Assert.NotNull(topic);
        Assert.NotNull(topic.Topic);
        Assert.NotEmpty(topic.Topic);
        
        await room.LeaveAsync();
    }
}