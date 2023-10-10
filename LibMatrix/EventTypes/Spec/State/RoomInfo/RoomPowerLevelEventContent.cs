using System.Text.Json.Serialization;
using LibMatrix.Interfaces;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.power_levels")]
public class RoomPowerLevelEventContent : EventContent {
    [JsonPropertyName("ban")]
    public long? Ban { get; set; } = 50;

    [JsonPropertyName("events_default")]
    public long EventsDefault { get; set; } = 0;

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
        return Users.TryGetValue(userId, out var level) && level >= Events.Max(x => x.Value);
    }

    public bool UserHasPermission(string userId, string eventType) {
        return Users.TryGetValue(userId, out var level) && level >= Events.GetValueOrDefault(eventType, EventsDefault);
    }
}
