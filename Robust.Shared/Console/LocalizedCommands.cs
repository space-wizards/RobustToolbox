using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console;

public abstract class LocalizedCommands : IConsoleCommand
{
    [Dependency] protected readonly SharedLocalizationManager LocalizationManager = default!;

    /// <inheritdoc />
    public abstract string Command { get; }

    protected virtual FText DescText => new($"cmd-{Command}-desc");
    protected virtual FText HelpText => new($"cmd-{Command}-help");

    /// <inheritdoc />
    public virtual string Description => LocalizationManager.TryGetString(DescText, out var val) ? val : DescText.Name;

    /// <inheritdoc />
    public virtual string Help => LocalizationManager.TryGetString(HelpText, out var val) ? val : HelpText.Name;

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
