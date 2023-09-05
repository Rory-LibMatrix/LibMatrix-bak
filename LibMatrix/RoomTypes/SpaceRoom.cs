using ArcaneLibs.Extensions;
using LibMatrix.Homeservers;

namespace LibMatrix.RoomTypes;

public class SpaceRoom : GenericRoom {
    private new readonly AuthenticatedHomeserverGeneric _homeserver;
    private readonly GenericRoom _room;

    public SpaceRoom(AuthenticatedHomeserverGeneric homeserver, string roomId) : base(homeserver, roomId) {
        _homeserver = homeserver;
    }

    private static SemaphoreSlim _semaphore = new(1, 1);
    public async IAsyncEnumerable<GenericRoom> GetRoomsAsync(bool includeRemoved = false) {
        await _semaphore.WaitAsync();
        var rooms = new List<GenericRoom>();
        var state = GetFullStateAsync();
        await foreach (var stateEvent in state) {
            if (stateEvent.Type != "m.space.child") continue;
            if (stateEvent.RawContent.ToJson() != "{}" || includeRemoved)
                yield return await _homeserver.GetRoom(stateEvent.StateKey);
        }
        _semaphore.Release();
    }
}
