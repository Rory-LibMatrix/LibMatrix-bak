namespace LibMatrix.ExampleBot.Bot.Interfaces;

public interface ICommand {
    public string Name { get; }
    public string Description { get; }

    public Task<bool> CanInvoke(CommandContext ctx) => Task.FromResult(true);

    public Task Invoke(CommandContext ctx);
}