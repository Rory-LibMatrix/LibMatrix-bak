using System.Text.Json;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes.Spec;

namespace LibMatrix.EventTypes.Abstractions.Tests;

public class SerialisationTests {
    private static readonly Dictionary<string, string> TestData = Directory.GetFiles("TestData").Where(x=>x.EndsWith(".json")).ToDictionary(Path.GetFileNameWithoutExtension, File.ReadAllText);
    [Fact]
    public void DeserializeEvent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(TestData["RoomMembershipEvent"]);
        Assert.NotNull(evt);
        Assert.NotNull(evt.Content);
        Assert.NotNull(evt.Content.Membership);
    }

    [Fact]
    public void DeserialiseEventContent() {
        var evt = JsonSerializer.Deserialize<RoomMembershipEventContent>(TestData["RoomMembershipEventContent"]);
        Assert.NotNull(evt);
        Assert.NotNull(evt.Membership);
    }
    
    [Fact]
    public void DeserializeUnknownEvent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<BaseMatrixEventContent>>(TestData["RoomMembershipEvent"]);
        Assert.NotNull(evt);
        Assert.NotNull(evt.Content);
        Assert.NotNull(evt.Content["membership"]);
    }
    
    [Fact]
    public void DeserializeUnknownEventContent() {
        var evt = JsonSerializer.Deserialize<BaseMatrixEventContent>(TestData["RoomMembershipEventContent"]);
        Assert.NotNull(evt);
        Assert.NotNull(evt["membership"]);
    }
    
    [Fact]
    public void SerializeEvent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(TestData["RoomMembershipEvent"]);
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("membership", json);
    }
    
    [Fact]
    public void SerializeEventContent() {
        var evt = JsonSerializer.Deserialize<RoomMembershipEventContent>(TestData["RoomMembershipEventContent"]);
        Assert.NotNull(evt);
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("membership", json);
    }
    
    [Fact]
    public void SerializeUnknownEvent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<BaseMatrixEventContent>>(TestData["RoomMembershipEvent"]);
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("membership", json);
    }
    
    [Fact]
    public void SerializeUnknownEventContent() {
        var evt = JsonSerializer.Deserialize<BaseMatrixEventContent>(TestData["RoomMembershipEventContent"]);
        Assert.NotNull(evt);
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("membership", json);
    }
    
    [Fact]
    public void ModifyEvent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(TestData["RoomMembershipEvent"]);
        Assert.NotNull(evt);
        Assert.NotNull(evt.Content);
        evt.Content.Membership = "meow";
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("meow", json);
    }
    
    [Fact]
    public void ModifyEventContent() {
        var evt = JsonSerializer.Deserialize<RoomMembershipEventContent>(TestData["RoomMembershipEventContent"]);
        Assert.NotNull(evt);
        evt.Membership = "meow";
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("meow", json);
    }
    
    [Fact]
    public void ModifyUnknownEvent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<BaseMatrixEventContent>>(TestData["RoomMembershipEvent"]);
        Assert.NotNull(evt);
        evt.Content["membership"] = "meow";
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("meow", json);
    }
    
    [Fact]
    public void ModifyUnknownEventContent() {
        var evt = JsonSerializer.Deserialize<BaseMatrixEventContent>(TestData["RoomMembershipEventContent"]);
        Assert.NotNull(evt);
        evt["membership"] = "meow";
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("meow", json);
    }
    
    [Fact]
    public void SerializeEventWithUnknownContent() {
        var evt = JsonSerializer.Deserialize<MatrixEvent<BaseMatrixEventContent>>(TestData["RoomMembershipEvent"]);
        Assert.NotNull(evt);
        var json = evt.ToJson();
        Assert.NotNull(json);
        Assert.Contains("membership", json);
    }
}