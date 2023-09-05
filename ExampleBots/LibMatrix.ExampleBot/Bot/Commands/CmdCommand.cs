using LibMatrix.ExampleBot.Bot.Interfaces;
using LibMatrix.StateEventTypes.Spec;

namespace LibMatrix.ExampleBot.Bot.Commands;

public class CmdCommand : ICommand {
    public string Name => "cmd";
    public string Description => "Runs a command on the host system";

    public Task<bool> CanInvoke(CommandContext ctx) {
        return Task.FromResult(ctx.MessageEvent.Sender.EndsWith(":rory.gay"));
    }

    public async Task Invoke(CommandContext ctx) {
        var cmd = ctx.Args.Aggregate("\"", (current, arg) => current + arg + " ");

        cmd = cmd.Trim();
        cmd += "\"";

        await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventData(body: $"Command being executed: `{cmd}`"));

        var output = ArcaneLibs.Util.GetCommandOutputSync(
                Environment.OSVersion.Platform == PlatformID.Unix ? "/bin/sh" : "cmd.exe",
                (Environment.OSVersion.Platform == PlatformID.Unix ? "-c " : "/c ") + cmd)
            .Replace("`", "\\`")
            .Split("\n").ToList();
        foreach (var _out in output) Console.WriteLine($"{_out.Length:0000} {_out}");

        var msg = "";
        while (output.Count > 0) {
            Console.WriteLine("Adding: " + output[0]);
            msg += output[0] + "\n";
            output.RemoveAt(0);
            if ((output.Count > 0 && (msg + output[0]).Length > 64000) || output.Count == 0) {
                await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
                    FormattedBody = $"```ansi\n{msg}\n```",
                    // Body = Markdig.Markdown.ToHtml(msg),
                    Format = "org.matrix.custom.html"
                });
                msg = "";
            }
        }
    }
}
