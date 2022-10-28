using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console;

public abstract class LocalizedCommands : IConsoleCommand
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    /// <inheritdoc />
    public abstract string Command { get; }

    /// <inheritdoc />
    public virtual string Description => _loc.TryGetString($"cmd-{Command}-desc", out var val) ? val : "";

    /// <inheritdoc />
    public virtual string Help => _loc.TryGetString($"cmd-{Command}-help", out var val) ? val : "";

    public abstract void Execute(IConsoleShell shell, string argStr, string[] args);
}
