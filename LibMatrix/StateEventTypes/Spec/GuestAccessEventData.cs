using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace LibMatrix.StateEventTypes.Spec;

[MatrixEvent(EventName = "m.room.guest_access")]
public class GuestAccessEventContent : EventContent {
    [JsonPropertyName("guest_access")]
    public string GuestAccess { get; set; }

    public bool IsGuestAccessEnabled {
        get => GuestAccess == "can_join";
        set => GuestAccess = value ? "can_join" : "forbidden";
    }
}
