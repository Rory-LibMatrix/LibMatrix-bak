using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Homeservers;
using LibMatrix.Responses;
using LibMatrix.RoomTypes;

namespace LibMatrix.Tests.Abstractions;

public static class RoomAbstraction {
    public static async Task<GenericRoom> GetTestRoom(AuthenticatedHomeserverGeneric hs) {
        var testRoom = await hs.CreateRoom(new CreateRoomRequest() {
            Name = "LibMatrix Test Room",
            // Visibility = CreateRoomVisibility.Public,
            RoomAliasName = Guid.NewGuid().ToString()
        });

        await testRoom.SendStateEventAsync("gay.rory.libmatrix.unit_test_room", new());

        return testRoom;
    }

    private static SemaphoreSlim _spaceSemaphore = null!;

    public static async Task<SpaceRoom> GetTestSpace(AuthenticatedHomeserverGeneric hs, int roomCount = 100, bool addSpaces = false, int spaceSizeReduction = 10) {
        _spaceSemaphore ??= new(roomCount / spaceSizeReduction, roomCount / spaceSizeReduction);
        var crq = new CreateRoomRequest() {
            Name = $"LibMatrix Test Space ({roomCount} children)",
            // Visibility = CreateRoomVisibility.Public,
            RoomAliasName = Guid.NewGuid().ToString(),
            InitialState = new()
        };
        crq._creationContentBaseType.Type = "m.space";


        var createRoomTasks = Enumerable.Range(0, roomCount)
            .Select(_ => hs.CreateRoom(new CreateRoomRequest() {
                Name = $"LibMatrix Test Room {Guid.NewGuid()}",
                // Visibility = CreateRoomVisibility.Public,
                RoomAliasName = Guid.NewGuid().ToString()
            })).ToAsyncEnumerable();

        await foreach (var room in createRoomTasks) {
            crq.InitialState.Add(new() {
                Type = "m.space.child",
                StateKey = room.RoomId,
                TypedContent = new SpaceChildEventContent() {
                    Via = new() {
                        room.RoomId.Split(":")[1]
                    }
                }
            });
        }

        if (addSpaces) {
            for (int i = 0; i < roomCount; i++) {
                var space = await GetTestSpace(hs, roomCount - spaceSizeReduction, true, spaceSizeReduction);
                crq.InitialState.Add(new() {
                    Type = "m.space.child",
                    StateKey = space.RoomId,
                    TypedContent = new SpaceChildEventContent() {
                        Via = new() {
                            space.RoomId.Split(":")[1]
                        }
                    }
                });
            }
        }

        var testSpace = (await hs.CreateRoom(crq)).AsSpace;

        await testSpace.SendStateEventAsync("gay.rory.libmatrix.unit_test_room", new());

        // _spaceSemaphore.Release();
        return testSpace;
    }
}
