using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;
using LibMatrix.Responses;

namespace LibMatrix.Tests.Abstractions;

public static class HomeserverAbstraction {
    public static async Task<AuthenticatedHomeserverGeneric> GetHomeserver() {
        var rhs = await RemoteHomeserver.Create("https://matrixunittests.rory.gay");
        // string username = Guid.NewGuid().ToString();
        // string password = Guid.NewGuid().ToString();
        var username = "@f1a2d2d6-1924-421b-91d0-893b347b2a49:matrixunittests.rory.gay";
        var password = "d6d782d6-8bc9-4fac-9cd8-78e101b4298b";
        LoginResponse reg;
        try {
            reg = await rhs.LoginAsync(username, password);
        }
        catch (MatrixException e) {
            if (e.ErrorCode == "M_FORBIDDEN") {
                await rhs.RegisterAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Unit tests!");
                reg = await rhs.RegisterAsync(username, password, "Unit tests!");
            }
            else throw new Exception("Failed to register", e);
        }

        var hs = await reg.GetAuthenticatedHomeserver("https://matrixunittests.rory.gay");

        //var rooms = await hs.GetJoinedRooms();

        // var disbandRoomTasks = rooms.Select(async room => {
        //     // await room.DisbandRoomAsync();
        //     await room.LeaveAsync();
        //     await room.ForgetAsync();
        //     return room;
        // }).ToList();
        // await Task.WhenAll(disbandRoomTasks);

        // foreach (var room in rooms) {
        //     // await room.DisbandRoomAsync();
        //     await room.LeaveAsync();
        //     await room.ForgetAsync();
        // }

        return hs;
    }

    public static async Task<AuthenticatedHomeserverGeneric> GetRandomHomeserver() {
        var rhs = await RemoteHomeserver.Create("https://matrixunittests.rory.gay");
        var reg = await rhs.RegisterAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Unit tests!");
        var hs = await reg.GetAuthenticatedHomeserver("https://matrixunittests.rory.gay");

        // var rooms = await hs.GetJoinedRooms();
        //
        // var disbandRoomTasks = rooms.Select(async room => {
        //     // await room.DisbandRoomAsync();
        //     await room.LeaveAsync();
        //     await room.ForgetAsync();
        //     return room;
        // }).ToList();
        // await Task.WhenAll(disbandRoomTasks);

        return hs;
    }

    public static async IAsyncEnumerable<AuthenticatedHomeserverGeneric> GetRandomHomeservers(int count = 1) {
        var createRandomUserTasks = Enumerable
            .Range(0, count)
            .Select(_ => GetRandomHomeserver()).ToAsyncEnumerable();
        await foreach (var hs in createRandomUserTasks) yield return hs;
    }
}