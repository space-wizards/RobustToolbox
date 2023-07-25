using System.Collections.Generic;
using System.Linq;
using Robust.Shared.RTShell.Syntax;

namespace Robust.Shared.RTShell.Commands.Info;

[ConsoleCommand]
internal sealed class CmdCommand : ConsoleCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List()
        => RtShell.AllCommands();

    [CommandImplementation("explain")]
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

    [CommandImplementation("suggest")]
    public void Suggest(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] CommandRun expr
    )
    {
        if (expr.Commands.Count == 0)
            return;

        var retTy = expr.Commands.Last().Item1.ReturnType;

        if (retTy is null)
        {
            ctx.WriteLine("The given expression is complete and doesn't have any way to extend it.");
        }
    }
}
