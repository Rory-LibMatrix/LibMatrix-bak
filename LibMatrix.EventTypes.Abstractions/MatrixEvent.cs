using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes;

public interface IBaseMatrixEvent {
    
}
public partial interface IMatrixEvent<out T> : IBaseMatrixEvent where T : BaseMatrixEventContent;
public class MatrixEvent<T> : IMatrixEvent<T> where T : BaseMatrixEventContent {
    [JsonPropertyName("content")]
    public T? Content { get; set; }
}