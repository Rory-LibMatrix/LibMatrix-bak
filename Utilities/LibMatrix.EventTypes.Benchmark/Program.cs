using System.Text.Json;
using ArcaneLibs.Extensions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Spec;

BenchmarkRunner.Run<Tests>();

[ShortRunJob]
[MemoryDiagnoser]
public class Tests {
    // public MatrixEventCollection<MatrixEventContent> Members = [
    //     new MatrixEvent<RoomMembershipEventContent>() {
    //         Content = new() {
    //             Membership = "join"
    //         }
    //     }
    // ];

    private static string eventJson = File.ReadAllText("test-event.json");
    private static MatrixEvent<RoomMembershipEventContent> evt2 = JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(eventJson);
    [Benchmark]
    public void Deserialise() {
        JsonSerializer.Deserialize<MatrixEvent<RoomMembershipEventContent>>(eventJson);
    }
    [Benchmark]
    public void Serialise() {
        evt2.ToJson();
    }
    
    [Benchmark]
    public void Modify() {
        evt2.Content.Membership = "meow";
    }
}