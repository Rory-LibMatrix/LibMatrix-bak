namespace LibMatrix.EventTypes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MatrixEventAttribute : Attribute {
    public required string EventName { get; set; }
    public bool Legacy { get; set; }
}
