using Robust.Shared.RTShell.Syntax;

namespace Robust.Shared.RTShell.Commands.Info;

[RtShellCommand]
public sealed class ExplainCommand : RtShellCommand
{
    [CommandImplementation]
    public void Explain(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] CommandRun expr
    )
    {
        foreach (var (cmd, span) in expr.Commands)
        {
            ctx.WriteLine(cmd.Command.GetHelp(cmd.SubCommand));
            ctx.WriteLine($"{cmd.PipedType?.PrettyName() ?? "[none]"} -> {cmd.ReturnType?.PrettyName() ?? "[none]"}");
        }
    }
}
