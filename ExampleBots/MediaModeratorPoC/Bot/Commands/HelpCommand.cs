using System.Text;
using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MediaModeratorPoC.Bot.Commands;

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

        await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
            MessageType = "m.notice",
            Body = sb.ToString()
        });
    }
}
