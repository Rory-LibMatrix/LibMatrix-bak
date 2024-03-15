using ArcaneLibs;
using LibMatrix.EventTypes.Spec;

namespace LibMatrix.Helpers;

public class MessageBuilder(string msgType = "m.text", string format = "org.matrix.custom.html") {
    private RoomMessageEventContent Content { get; set; } = new() {
        MessageType = msgType,
        Format = format
    };

    public RoomMessageEventContent Build() => Content;

    public MessageBuilder WithBody(string body) {
        Content.Body += body;
        Content.FormattedBody += body;
        return this;
    }

    public MessageBuilder WithHtmlTag(string tag, string body, Dictionary<string, string>? attributes = null) {
        Content.Body += body;
        Content.FormattedBody += $"<{tag}";
        if (attributes != null)
            foreach (var (key, value) in attributes)
                Content.FormattedBody += $" {key}=\"{value}\"";
        Content.FormattedBody += $">{body}</{tag}>";
        return this;
    }

    public MessageBuilder WithHtmlTag(string tag, Action<MessageBuilder> bodyBuilder, Dictionary<string, string>? attributes = null) {
        Content.FormattedBody += $"<{tag}";
        if (attributes != null)
            foreach (var (key, value) in attributes)
                Content.FormattedBody += $" {key}=\"{value}\"";
        Content.FormattedBody += ">";
        bodyBuilder(this);
        Content.FormattedBody += $"</{tag}>";
        return this;
    }

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
    
    public MessageBuilder WithCustomEmoji(string mxcUri, string name) {
        Content.Body += $"{{{name}}}";
        Content.FormattedBody += $"<img data-mx-emoticon height=\"32\" src=\"{mxcUri}\" alt=\"{name}\" title=\"{name}\" />";
        return this;
    }

    public MessageBuilder WithRainbowString(string text, byte skip = 1, int offset = 0, double lengthFactor = 255.0, bool useLength = true) {
        if (useLength) {
            lengthFactor = text.Length;
        }

        // HslaColorInterpolator interpolator = new((0, 255, 128, 255), (255, 255, 128, 255));
        // // RainbowEnumerator enumerator = new(skip, offset, lengthFactor);
        // for (int i = 0; i < text.Length; i++) {
        //     // var (r, g, b) = enumerator.Next();
        //     // var (r,g,b,a) = interpolator.Interpolate((i+offset) * skip, lengthFactor).ToRgba();
        //     // Console.WriteLine($"RBA: {r} {g} {b} {a}");
        //     // Content.FormattedBody += $"<font color=\"#{r:X2}{g:X2}{b:X2}\">{text[i]}</font>";
        // }
        return this;
    }
    
    public MessageBuilder WithCodeBlock(string code, string language = "plaintext") {
        Content.Body += code;
        Content.FormattedBody += $"<pre><code class=\"language-{language}\">{code}</code></pre>";
        return this;
    }
}