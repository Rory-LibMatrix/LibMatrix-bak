using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;

namespace LibMatrix.EventTypes;

// <T> : MatrixEventContent where T : MatrixEventContent<T>, new() {
/// <summary>
///     Extensible Event Content, aims to provide an API similar to JsonNode/JsonObject
///     <seealso cref="System.Text.Json.Nodes.JsonNode"/>
///     <seealso cref="System.Text.Json.Nodes.JsonObject"/>
/// </summary>
[JsonConverter(typeof(MatrixEventContentConverter<MatrixEventContent>))]
public class MatrixEventContent {

    [JsonExtensionData, JsonInclude]
    public JsonObject InternalJson { get; set; } = new();

    

    public MatrixEventContent() { }

    public MatrixEventContent(JsonNode json) {
        InternalJson = json.AsObject();
    }

    public static implicit operator MatrixEventContent(JsonNode json) => new(json);

    // public static implicit operator JsonNode(MatrixEventContent content) => content.InternalJson;

    [JsonIgnore]
    public IEnumerable<string> EventTypes => this.GetType().GetCustomAttributes<MatrixEventAttribute>().Select(x => x.EventType);

    [JsonIgnore]
    public string EventType => EventTypes.First();

    public JsonNode? this[string key] => InternalJson[key];
    
    public string ToJson() => InternalJson.ToJson();


    public class MatrixEventContentConverter<T> : JsonConverter<T> where T : MatrixEventContent, new() {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // read entire object into a JsonObject
            var json = JsonNode.Parse(ref reader);
            return new T { InternalJson = json.AsObject() };
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
            value.InternalJson.WriteTo(writer);
        }
    }
}

public class MatrixEventAttribute(string eventType, bool deprecated = false) : Attribute {
    public string EventType { get; } = eventType;
    public bool Deprecated { get; } = deprecated;
}