using LibMatrix.HomeserverEmulator.Services;

namespace LibMatrix.HomeserverEmulator.Extensions;

public static class EventExtensions {
    public static LegacyMatrixEventResponse ToStateEvent(this LegacyMatrixEvent legacyMatrixEvent, UserStore.User user, RoomStore.Room room) {
        return new LegacyMatrixEventResponse {
            RawContent = legacyMatrixEvent.RawContent,
            EventId = "$" + string.Join("", Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyzABCDEFGHIJLKMNOPQRSTUVWXYZ0123456789".ToCharArray(), 100)),
            RoomId = room.RoomId,
            Sender = user.UserId,
            StateKey = legacyMatrixEvent.StateKey,
            Type = legacyMatrixEvent.Type,
            OriginServerTs = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };
    }

    public static List<LegacyMatrixEventResponse> GetCalculatedState(this IEnumerable<LegacyMatrixEventResponse> events) {
        return events.Where(s => s.StateKey != null)
            .OrderByDescending(s => s.OriginServerTs)
            .DistinctBy(x => (x.Type, x.StateKey))
            .ToList();
    }
}