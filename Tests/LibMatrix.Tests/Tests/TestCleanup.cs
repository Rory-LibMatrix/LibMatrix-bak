// using System.Diagnostics;
// using LibMatrix.Helpers;
// using LibMatrix.Services;
// using LibMatrix.Tests.Abstractions;
// using LibMatrix.Tests.Fixtures;
// using Microsoft.Extensions.Logging;
// using Xunit.Abstractions;
// using Xunit.Microsoft.DependencyInjection.Abstracts;
//
// namespace LibMatrix.Tests.Tests;
//
// public class TestCleanup : TestBed<TestFixture> {
//     private readonly HomeserverAbstraction _hsAbstraction;
//     private readonly ILogger<TestCleanup> _logger;
//
//     public TestCleanup(ITestOutputHelper testOutputHelper, TestFixture fixture) : base(testOutputHelper, fixture) {
//         // _fixture = fixture;
//         _logger = _fixture.GetService<ILogger<TestCleanup>>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(ILogger<TestCleanup>)}");
//         _hsAbstraction = _fixture.GetService<HomeserverAbstraction>(_testOutputHelper) ?? throw new InvalidOperationException($"Failed to get {nameof(HomeserverAbstraction)}");
//     }
//
//     [SkippableFact(typeof(MatrixException))]
//     public async Task Cleanup() {
//         // Assert.False(string.IsNullOrWhiteSpace(_config.TestHomeserver), $"{nameof(_config.TestHomeserver)} must be set in appsettings!");
//         // Assert.False(string.IsNullOrWhiteSpace(_config.TestUsername), $"{nameof(_config.TestUsername)} must be set in appsettings!");
//         // Assert.False(string.IsNullOrWhiteSpace(_config.TestPassword), $"{nameof(_config.TestPassword)} must be set in appsettings!");
//
//         var hs = await _hsAbstraction.GetConfiguredHomeserver();
//         Assert.NotNull(hs);
//
//         var syncHelper = new SyncHelper(hs, _logger) {
//             Timeout = 3000
//         };
//         _testOutputHelper.WriteLine("Starting sync loop");
//         var cancellationTokenSource = new CancellationTokenSource();
//         var sw = Stopwatch.StartNew();
//         syncHelper.SyncReceivedHandlers.Add(async response => {
//             // if (sw.ElapsedMilliseconds >= 3000) {
//                 // _testOutputHelper.WriteLine("Cancelling sync loop");
//
//                 var tasks = (await hs.GetJoinedRooms()).Select(async room => {
//                     _logger.LogInformation("Leaving room: {}", room.RoomId);
//                     await room.LeaveAsync();
//                     await room.ForgetAsync();
//                     return room;
//                 }).ToList();
//                 await Task.WhenAll(tasks);
//
//                 // cancellationTokenSource.Cancel();
//             // }
//
//             sw.Restart();
//             if (response.Rooms?.Leave is { Count: > 0 }) {
//                 // foreach (var room in response.Rooms.Leave) {
//                 // await hs.GetRoom(room.Key).ForgetAsync();
//                 // }
//                 var tasks2 = response.Rooms.Leave.Select(async room => {
//                     await hs.GetRoom(room.Key).ForgetAsync();
//                     return room;
//                 }).ToList();
//                 await Task.WhenAll(tasks2);
//             }
//         });
//         await syncHelper.RunSyncLoopAsync(cancellationToken: cancellationTokenSource.Token);
//
//         Assert.NotNull(hs);
//         await hs.Logout();
//     }
// }