using ArcaneLibs;
using LibMatrix.EventTypes.Spec;

namespace LibMatrix.Helpers;

public class MessageBuilder(string msgType = "m.text", string format = "org.matrix.custom.html") {
    private RoomMessageEventContent Content { get; set; } = new() {
        MessageType = msgType,
        Format = format
    };
    
    public RoomMessageEventContent Build() => Content;
    
    public MessageBuilder WithColoredBody(string color, string body) {
        Content.Body += body;
        Content.FormattedBody += $"<font color=\"{color}\">{body}</font>";
        return this;
    }
    
    public MessageBuilder WithColoredBody(string color, Action<MessageBuilder> bodyBuilder) {
        Content.FormattedBody += $"<font color=\"{color}\">";
        bodyBuilder(this);
        Content.FormattedBody += "</font>";
        return this;
    }

    public MessageBuilder WithRainbowString(string text, byte skip = 1, int offset = 0, double lengthFactor = 255.0, bool useLength = true) {
        if (useLength) {
            lengthFactor = text.Length;
        }
        RainbowEnumerator enumerator = new(skip, offset, lengthFactor);
        for (int i = 0; i < text.Length; i++) {
            var (r, g, b) = enumerator.Next();
            Content.FormattedBody += $"<font color=\"#{r:X2}{g:X2}{b:X2}\">{text[i]}</font>";
        }

        return this;
    }
    
}