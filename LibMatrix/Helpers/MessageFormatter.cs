using ArcaneLibs.Extensions;
using LibMatrix.LegacyEvents.EventTypes.Spec;

namespace LibMatrix.Helpers;

public static class MessageFormatter {
    public static RoomMessageLegacyEventContent FormatError(string error) =>
        new(body: error, messageType: "m.text") {
            FormattedBody = $"<font color=\"#EE4444\">{error}</font>",
            Format = "org.matrix.custom.html"
        };

    public static RoomMessageLegacyEventContent FormatException(string error, Exception e) =>
        new(body: $"{error}: {e.Message}", messageType: "m.text") {
            FormattedBody = $"<font color=\"#EE4444\">{error}: <pre><code>{e.Message}</code></pre></font>",
            Format = "org.matrix.custom.html"
        };

    public static RoomMessageLegacyEventContent FormatSuccess(string text) =>
        new(body: text, messageType: "m.text") {
            FormattedBody = $"<font color=\"#00FF00\">{text}</font>",
            Format = "org.matrix.custom.html"
        };

    public static RoomMessageLegacyEventContent FormatSuccessJson(string text, object data) =>
        new(body: text, messageType: "m.text") {
            FormattedBody = $"<font color=\"#00FF00\">{text}: <pre><code>{data.ToJson(ignoreNull: true)}</code></pre></font>",
            Format = "org.matrix.custom.html"
        };

    public static string HtmlFormatMention(string id, string? displayName = null) => $"<a href=\"https://matrix.to/#/{id}\">{displayName ?? id}</a>";

    public static string HtmlFormatMessageLink(string roomId, string eventId, string[]? servers = null, string? displayName = null) {
        if (servers is not { Length: > 0 }) servers = new[] { roomId.Split(':', 2)[1] };
        return $"<a href=\"https://matrix.to/#/{roomId}/{eventId}?via={string.Join("&via=", servers)}\">{displayName ?? eventId}</a>";
    }

#region Extension functions

    public static RoomMessageLegacyEventContent ToMatrixMessage(this Exception e, string error) => FormatException(error, e);

#endregion

    public static RoomMessageLegacyEventContent FormatWarning(string warning) =>
        new(body: warning, messageType: "m.text") {
            FormattedBody = $"<font color=\"#FFFF00\">{warning}</font>",
            Format = "org.matrix.custom.html"
        };

    public static RoomMessageLegacyEventContent FormatWarningJson(string warning, object data) =>
        new(body: warning, messageType: "m.text") {
            FormattedBody = $"<font color=\"#FFFF00\">{warning}: <pre><code>{data.ToJson(ignoreNull: true)}</code></pre></font>",
            Format = "org.matrix.custom.html"
        };

    public static RoomMessageLegacyEventContent Concat(this RoomMessageLegacyEventContent a, RoomMessageLegacyEventContent b) =>
        new(body: $"{a.Body}{b.Body}", messageType: a.MessageType) {
            FormattedBody = $"{a.FormattedBody}{b.FormattedBody}",
            Format = a.Format
        };

    public static RoomMessageLegacyEventContent ConcatLine(this RoomMessageLegacyEventContent a, RoomMessageLegacyEventContent b) =>
        new(body: $"{a.Body}\n{b.Body}", messageType: "m.text") {
            FormattedBody = $"{a.FormattedBody}<br/>{b.FormattedBody}",
            Format = "org.matrix.custom.html"
        };
}