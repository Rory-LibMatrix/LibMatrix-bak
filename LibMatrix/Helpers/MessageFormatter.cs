using ArcaneLibs.Extensions;
using LibMatrix.StateEventTypes.Spec;

namespace LibMatrix.Helpers;

public static class MessageFormatter {
    public static RoomMessageEventData FormatError(string error) {
        return new RoomMessageEventData(body: error, messageType: "m.text") {
            FormattedBody = $"<font color=\"#FF0000\">{error}: {error}</font>",
            Format = "org.matrix.custom.html"
        };
    }

    public static RoomMessageEventData FormatException(string error, Exception e) {
        return new RoomMessageEventData(body: $"{error}: {e.Message}", messageType: "m.text") {
            FormattedBody = $"<font color=\"#FF0000\">{error}: <pre>{e.Message}</pre>" +
                            $"</font>",
            Format = "org.matrix.custom.html"
        };
    }

    public static RoomMessageEventData FormatSuccess(string text) {
        return new RoomMessageEventData(body: text, messageType: "m.text") {
            FormattedBody = $"<font color=\"#00FF00\">{text}</font>",
            Format = "org.matrix.custom.html"
        };
    }

    public static RoomMessageEventData FormatSuccessJson(string text, object data) {
        return new RoomMessageEventData(body: text, messageType: "m.text") {
            FormattedBody = $"<font color=\"#00FF00\">{text}: <pre>{data.ToJson(ignoreNull: true)}</pre></font>",
            Format = "org.matrix.custom.html"
        };
    }

    public static string HtmlFormatMention(string id, string? displayName = null) {
        return $"<a href=\"https://matrix.to/#/{id}\">{displayName ?? id}</a>";
    }
}
