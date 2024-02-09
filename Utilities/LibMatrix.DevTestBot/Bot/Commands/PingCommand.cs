using LibMatrix.EventTypes.Spec;
using LibMatrix.ExampleBot.Bot.Interfaces;

namespace LibMatrix.ExampleBot.Bot.Commands;

public class PingCommand : ICommand {
    public string Name { get; } = "ping";
    public string Description { get; } = "Pong!";

    public async Task Invoke(CommandContext ctx) => await ctx.Room.SendMessageEventAsync(new RoomMessageEventContent(body: "pong!"));
}