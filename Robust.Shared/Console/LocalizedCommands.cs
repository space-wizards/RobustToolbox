using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console;

public abstract class LocalizedCommands : IConsoleCommand
{
    [Dependency] protected readonly ILocalizationManager LocalizationManager = default!;

    /// <inheritdoc />
    public abstract string Command { get; }

    /// <inheritdoc />
    public virtual string Description => LocalizationManager.TryGetString($"cmd-{Command}-desc", out var val) ? val : "";

    /// <inheritdoc />
    public virtual string Help => LocalizationManager.TryGetString($"cmd-{Command}-help", out var val) ? val : "";

    /// <inheritdoc />
    public virtual bool RequireServerOrSingleplayer => false;

    /// <inheritdoc />
    public abstract void Execute(IConsoleShell shell, string argStr, string[] args);

    /// <inheritdoc />
    public virtual CompletionResult GetCompletion(IConsoleShell shell, string[] args) => CompletionResult.Empty;

    /// <inheritdoc />
    public virtual ValueTask<CompletionResult> GetCompletionAsync(IConsoleShell shell, string[] args, string argStr,
        CancellationToken cancel)
    {
        return ValueTask.FromResult(GetCompletion(shell, args));
    }
}

/// <summary>
/// Base class for localized console commands that run in "entity space".
/// </summary>
/// <remarks>
/// <para>
/// This type of command is registered only while the entity system is active.
/// On the client this means that the commands are only available while connected to a server or in single player.
/// </para>
/// <para>
/// These commands are allowed to take dependencies on entity systems, reducing boilerplate for many usages.
/// </para>
/// </remarks>
public abstract class LocalizedEntityCommands : LocalizedCommands, IEntityConsoleCommand
{
    [Dependency]
    protected readonly EntityManager EntityManager = default!;
}
