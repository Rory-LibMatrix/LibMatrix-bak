using LibMatrix.HomeserverEmulator.Services;

namespace LibMatrix.HomeserverEmulator.Extensions;

public static class EventExtensions {
    public static StateEventResponse ToStateEvent(this StateEvent stateEvent, UserStore.User user, RoomStore.Room room) {
        return new StateEventResponse {
            RawContent = stateEvent.RawContent,
            EventId = "$" + string.Join("", Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyzABCDEFGHIJLKMNOPQRSTUVWXYZ0123456789".ToCharArray(), 100)),
            RoomId = room.RoomId,
            Sender = user.UserId,
            StateKey = stateEvent.StateKey,
            Type = stateEvent.Type,
            OriginServerTs = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };
    }

    public static List<StateEventResponse> GetCalculatedState(this IEnumerable<StateEventResponse> events) {
        return events.Where(s => s.StateKey != null)
            .OrderByDescending(s => s.OriginServerTs)
            .DistinctBy(x => (x.Type, x.StateKey))
            .ToList();
    }
}