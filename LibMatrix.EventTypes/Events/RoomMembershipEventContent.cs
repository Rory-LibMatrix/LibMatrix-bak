using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Events;

public class RoomMembershipEventContent : MatrixEventContent {
    public string Membership {
        get => _json["membership"]!.GetValue<string>();
        set => Console.WriteLine(value);
    }
}