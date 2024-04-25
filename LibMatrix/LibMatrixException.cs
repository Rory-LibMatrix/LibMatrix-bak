using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;

namespace LibMatrix;

public class LibMatrixException : Exception {
    [JsonPropertyName("errcode")]
    public required string ErrorCode { get; set; }

    [JsonPropertyName("error")]
    public required string Error { get; set; }


    public object GetAsObject() => new { errcode = ErrorCode, error = Error };
    public string GetAsJson() => GetAsObject().ToJson(ignoreNull: true);

    public override string Message =>
        $"{ErrorCode}: {ErrorCode switch {
            "M_UNSUPPORTED" => "The requested feature is not supported",
            _ => $"Unknown error: {GetAsObject().ToJson(ignoreNull: true)}"
        }}\nError: {Error}";

    public static class ErrorCodes {
        public const string M_NOT_FOUND = "M_NOT_FOUND";
        public const string M_UNSUPPORTED = "M_UNSUPPORTED";
    }
}