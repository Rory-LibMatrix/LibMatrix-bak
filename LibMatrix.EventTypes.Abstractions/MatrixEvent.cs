using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes;

public interface IMatrixEvent<out T> where T : BaseMatrixEventContent;
public class MatrixEvent<T> : IMatrixEvent<T> where T : BaseMatrixEventContent {
    [JsonPropertyName("content")]
    public T? Content { get; set; }
}