using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes;

public interface IMatrixEvent<out T> where T : MatrixEventContent;
public class MatrixEvent<T> : IMatrixEvent<T> where T : MatrixEventContent {
    [JsonPropertyName("content")]
    public T? Content { get; set; }
}