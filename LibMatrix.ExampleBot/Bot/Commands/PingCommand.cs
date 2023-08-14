using LibMatrix.ExampleBot.Bot.Interfaces;

namespace LibMatrix.ExampleBot.Bot.Commands; 

public class PingCommand : ICommand {
    public PingCommand() {
    }

    public string Name { get; } = "ping";
    public string Description { get; } = "Pong!";

    public async Task Invoke(CommandContext ctx) {
        await ctx.Room.SendMessageEventAsync("m.room.message", new() {
            Body = "pong!"
        });
    }
}