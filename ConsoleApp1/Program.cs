// See https://aka.ms/new-console-template for more information

using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Events;

Console.WriteLine("Hello, World!");

MatrixEventCollection collection = new();
collection.Add(new MatrixEvent<RoomMembershipEventContent>() {
    Content = new RoomMembershipEventContent() {
        Membership = "yes"
    }
});

List<MatrixEvent<MatrixEventContent>> collection2 = new();
collection2.Add(new MatrixEvent<RoomMembershipEventContent>() {
    Content = new RoomMembershipEventContent() {
        Membership = "yes"
    }
});

List<MatrixEventContent> collection3 = new();
collection3.Add(new RoomMembershipEventContent() {
    Membership = "yes"
});