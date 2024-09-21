using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class ExplainCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Explain(
            IInvocationContext ctx,
            CommandRun expr
        )
    {
        foreach (var (cmd, _) in expr.Commands)
        {
            ctx.WriteLine(cmd.Command.GetHelp(cmd.SubCommand));
            ctx.WriteLine($"{cmd.PipedType?.PrettyName() ?? "[none]"} -> {cmd.ReturnType?.PrettyName() ?? "[none]"}");
            ctx.WriteLine("");
        }
    }
}
