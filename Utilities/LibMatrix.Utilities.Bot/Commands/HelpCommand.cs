using System.Text;
using LibMatrix.EventTypes.Spec;
using LibMatrix.Utilities.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LibMatrix.Utilities.Bot.Commands;

public class HelpCommand(IServiceProvider services) : ICommand {
    public string Name { get; } = "help";
    public string Description { get; } = "Displays this help message";

    public async Task Invoke(CommandContext ctx) {
        var sb = new StringBuilder();
        sb.AppendLine("Available commands:");
        var commands = services.GetServices<ICommand>().ToList();
        foreach (var command in commands) {
            sb.AppendLine($"- {command.Name}: {command.Description}");
        }

        await ctx.Room.SendMessageEventAsync(new RoomMessageEventContent(messageType: "m.notice", body: sb.ToString()));
    }
}
