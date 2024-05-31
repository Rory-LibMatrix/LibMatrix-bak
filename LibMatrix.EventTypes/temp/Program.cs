using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Events;

namespace LibMatrix.EventTypes.temp;

public class Program {
    // public MatrixEventCollection<MatrixEventContent> Members = [
    //     new MatrixEvent<RoomMembershipEventContent>() {
    //         Content = new() {
    //             Membership = "join"
    //         }
    //     }
    // ];

    public static void Main(string[] args) {
        var evt = new RoomMembershipEventContent() {
            Membership = "join"
        };
        Console.WriteLine(evt.ToJson());

        var eventJson = File.ReadAllText("test-event.json");
        var evt2 = JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(eventJson);
        evt2.Content.Membership = "meow";
        Console.WriteLine(evt2.Content.ToJson());
        Console.WriteLine(ObjectExtensions.ToJson(evt2));

    }
}