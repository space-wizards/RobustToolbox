using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

/// <summary>
/// Proxy commands for invoking a toolshed run via the normal shell.
/// </summary>
[Reflect(false)]
public sealed class ToolshedProxyCommand : IConsoleCommand
{
    private readonly ToolshedManager _shed;

    internal ToolshedProxyCommand(CommandSpec spec, ToolshedManager shed)
    {
        _shed = shed;
        Spec = spec;
        Command = Spec.FullName();
    }

    public readonly CommandSpec Spec;
    public string Command { get; }
    public string Description => Spec.Cmd.Description(Spec.SubCommand);
    public string Help => Spec.Cmd.GetHelp(Spec.SubCommand);

    // Always forward to server.
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _shed.InvokeCommand(shell, argStr, null, out var result, out var ctx);

        if (!ctx.HasErrors)
        {
            var resultStr = _shed.PrettyPrintType(result, out var more, moreUsed: true);
            shell.WriteLine(FormattedMessage.FromMarkupPermissive(resultStr));
            ctx.WriteVar("more", more);
            return;
        }

        foreach (var err in ctx.GetErrors())
        {
            ctx.WriteLine(err.Describe());
        }

        // It is important that we add a WriteError instead of just using custom formatted text to ensure that things
        // like tests know an error was logged.
        shell.WriteError("Failed to execute toolshed command");
    }

    public ValueTask<CompletionResult> GetCompletionAsync(
        IConsoleShell shell,
        string[] args,
        string argStr,
        CancellationToken cancel)
    {
        return ValueTask.FromResult(_shed.GetCompletions(shell, argStr) ?? CompletionResult.Empty);
    }
}
