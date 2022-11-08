using System.Threading;
using System.Threading.Tasks;
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
    public abstract void Execute(IConsoleShell shell, string argStr, string[] args);
    
    /// <inheritdoc />
    public virtual CompletionResult GetCompletion(IConsoleShell shell, string[] args) => CompletionResult.Empty;

    /// <inheritdoc />
    public virtual ValueTask<CompletionResult> GetCompletionAsync(IConsoleShell shell, string[] args,
        CancellationToken cancel)
    {
        return ValueTask.FromResult(GetCompletion(shell, args));
    }
}
