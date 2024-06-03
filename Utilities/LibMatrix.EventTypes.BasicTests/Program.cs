// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Abstractions;
using LibMatrix.EventTypes.Spec;

Console.WriteLine("Hello, World!");

MatrixEventCollection Members = [
     new MatrixEvent<RoomMembershipEventContent>() {
         Content = new() {
             Membership = "join"
         }
     }
];

string eventJson = File.ReadAllText("test-event.json");
MatrixEvent<RoomMembershipEventContent> evt2 = JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(eventJson);

JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(eventJson);

evt2.ToJson();
evt2.Content.Membership = "meow";

MatrixEventCollection collection = new();
collection.Add(new MatrixEvent<RoomMembershipEventContent>() {
    Content = new RoomMembershipEventContent() {
        Membership = "yes"
    }
});
MatrixEventCollection<RoomMembershipEventContent> collection4 = new();
collection4.Add(new MatrixEvent<RoomMembershipEventContent>() {
    Content = new RoomMembershipEventContent() {
        Membership = "yes"
    }
});

List<MatrixEvent<BaseMatrixEventContent>> collection2 = new();
collection2.Add(new MatrixEvent<RoomMembershipEventContent>() {
    Content = new RoomMembershipEventContent() {
        Membership = "yes"
    }
});

List<BaseMatrixEventContent> collection3 = new();
collection3.Add(new RoomMembershipEventContent() {
    Membership = "yes"
});