using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes;

/// <summary>
///     Extensible Event Content, aims to provide an API similar to JsonNode/JsonObject
///     <seealso cref="System.Text.Json.Nodes.JsonNode"/>
///     <seealso cref="System.Text.Json.Nodes.JsonObject"/>
/// </summary>
public class MatrixEventContent {
    // <T> : MatrixEventContent where T : MatrixEventContent<T>, new() {
    internal JsonNode _json = new JsonObject();

    public static implicit operator MatrixEventContent(JsonNode json) => new(json);

    [JsonConstructor]
    public MatrixEventContent(JsonNode json) {
        _json = json;
    }

    public MatrixEventContent() { }

    public JsonNode? this[string key] => _json[key];
}