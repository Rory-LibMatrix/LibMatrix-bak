using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = EventId)]
public class RoomPowerLevelEventContent : EventContent {
    public const string EventId = "m.room.power_levels";

    [JsonPropertyName("ban")]
    public long? Ban { get; set; } = 50;

    [JsonPropertyName("events_default")]
    public long? EventsDefault { get; set; } = 0;

    [JsonPropertyName("events")]
    public Dictionary<string, long>? Events { get; set; } // = null!;

    [JsonPropertyName("invite")]
    public long? Invite { get; set; } = 0;

    [JsonPropertyName("kick")]
    public long? Kick { get; set; } = 50;

    [JsonPropertyName("notifications")]
    public NotificationsPL? NotificationsPl { get; set; } // = null!;

    [JsonPropertyName("redact")]
    public long? Redact { get; set; } = 50;

    [JsonPropertyName("state_default")]
    public long? StateDefault { get; set; } = 50;

    [JsonPropertyName("users")]
    public Dictionary<string, long>? Users { get; set; } // = null!;

    [JsonPropertyName("users_default")]
    public long? UsersDefault { get; set; } = 0;

    [Obsolete("Historical was a key related to MSC2716, a spec change on backfill that was dropped!", true)]
    [JsonIgnore]
    [JsonPropertyName("historical")]
    public long Historical { get; set; } // = 50;

    public class NotificationsPL {
        [JsonPropertyName("room")]
        public long Room { get; set; } = 50;
    }

    public bool IsUserAdmin(string userId) {
        if(userId is null) throw new ArgumentNullException(nameof(userId));
        return Users.TryGetValue(userId, out var level) && level >= Events.Max(x => x.Value);
    }

    public bool UserHasPermission(string userId, string eventType) {
        if(userId is null) throw new ArgumentNullException(nameof(userId));
        return Users.TryGetValue(userId, out var level) && level >= Events.GetValueOrDefault(eventType, EventsDefault ?? 0);
    }

    public long GetUserPowerLevel(string userId) {
        if(userId is null) throw new ArgumentNullException(nameof(userId));
        return Users.TryGetValue(userId, out var level) ? level : UsersDefault ?? UsersDefault ?? 0;
    }

    public long GetEventPowerLevel(string eventType) {
        return Events.TryGetValue(eventType, out var level) ? level : EventsDefault ?? EventsDefault ?? 0;
    }

    public void SetUserPowerLevel(string userId, long powerLevel) {
        if(userId is null) throw new ArgumentNullException(nameof(userId));
        Users ??= new();
        if (Users.TryGetValue(userId, out var level)) {
            Users[userId] = powerLevel;
        }
        else {
            Users.Add(userId, powerLevel);
        }
    }
}
