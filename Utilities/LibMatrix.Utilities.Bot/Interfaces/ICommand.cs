using System.Collections.Frozen;
using System.Collections.Immutable;

namespace LibMatrix.Utilities.Bot.Interfaces;

public interface ICommand {
    public string Name { get; }
    public string[]? Aliases { get; }
    public string Description { get; }
    public bool Unlisted { get; }

    public Task<bool> CanInvoke(CommandContext ctx) => Task.FromResult(true);

    public Task Invoke(CommandContext ctx);
}


public interface ICommandGroup : ICommand {
    public IImmutableList<ICommand> SubCommands { get; }
}