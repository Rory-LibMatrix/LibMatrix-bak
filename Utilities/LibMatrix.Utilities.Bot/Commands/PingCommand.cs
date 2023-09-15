using LibMatrix.StateEventTypes.Spec;
using MediaModeratorPoC.Bot.Interfaces;

namespace MediaModeratorPoC.Bot.Commands;

public class PingCommand : ICommand {
    public string Name { get; } = "ping";
    public string Description { get; } = "Pong!";

    public async Task Invoke(CommandContext ctx) {
        await ctx.Room.SendMessageEventAsync("m.room.message", new RoomMessageEventContent(body: "pong!"));
    }
}
