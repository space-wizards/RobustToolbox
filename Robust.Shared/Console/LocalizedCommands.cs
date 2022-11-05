using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console;

public abstract class LocalizedCommands : IConsoleCommand
{
    [Dependency] protected readonly ILocalizationManager LocalizationManager = default!;

    /// <inheritdoc />
    public virtual string Command
    {
        get
        {
            var className = GetType().Name;
            if (className.EndsWith("Command") && className.Length > 7)
                className = className.Substring(0, className.Length - 7);
            return className.ToLowerInvariant();
        }
    }
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
