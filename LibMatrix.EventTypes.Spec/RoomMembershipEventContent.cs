using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes.Events;

[MatrixEvent("m.room.member")]
[JsonConverter(typeof(MatrixEventContentConverter<RoomMembershipEventContent>))]
public class RoomMembershipEventContent : BaseMatrixEventContent {
    public string Membership {
        get => InternalJson["membership"]!.GetValue<string>();
        set => InternalJson["membership"] = value;
    }
    
    public string? Something { get; set; }
}