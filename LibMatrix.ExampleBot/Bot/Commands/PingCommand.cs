using LibMatrix.ExampleBot.Bot.Interfaces;
using LibMatrix.StateEventTypes.Spec;

namespace LibMatrix.ExampleBot.Bot.Commands;

public class PingCommand : ICommand {
    public string Name { get; } = "ping";
    public string Description { get; } = "Pong!";

    public async Task Invoke(CommandContext ctx) {
        await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventData {
            Body = "pong!"
        });
    }
}
