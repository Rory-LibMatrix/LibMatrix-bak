using System.Text.Json.Serialization;

namespace LibMatrix.EventTypes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MatrixEventAttribute(string eventType, bool deprecated = false) : Attribute {
    public string EventType { get; } = eventType;
    public bool Deprecated { get; } = deprecated;
}