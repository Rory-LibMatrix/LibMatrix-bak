using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Spec.State;

[MatrixEvent(EventName = "m.room.guest_access")]
public class RoomGuestAccessEventContent : TimelineEventContent {
    [JsonPropertyName("guest_access")]
    public required string GuestAccess { get; set; }

    [JsonIgnore]
    public bool IsGuestAccessEnabled {
        get => GuestAccess == "can_join";
        set => GuestAccess = value ? "can_join" : "forbidden";
    }
}
