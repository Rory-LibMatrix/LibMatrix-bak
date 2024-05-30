using System.Text.Json.Serialization;

namespace LibMatrix.LegacyEvents.EventTypes.Spec.State;

[LegacyMatrixEvent(EventName = EventId)]
public class RoomGuestAccessLegacyEventContent : LegacyEventContent {
    public const string EventId = "m.room.guest_access";

    [JsonPropertyName("guest_access")]
    public string GuestAccess { get; set; }

    [JsonIgnore]
    public bool IsGuestAccessEnabled {
        get => GuestAccess == "can_join";
        set => GuestAccess = value ? "can_join" : "forbidden";
    }
}