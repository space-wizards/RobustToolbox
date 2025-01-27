using System.Text;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

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
        var builder = new StringBuilder();
        foreach (var (cmd, _) in expr.Commands)
        {
            var name = cmd.Implementor.FullName;
            builder.AppendLine($"{name} - {cmd.Implementor.Description()}");

            if (cmd.PipedType != null)
            {
                var pipeArg = cmd.Method.Base.PipeArg;
                DebugTools.AssertNotNull(pipeArg);
                builder.Append($"<{pipeArg?.Name} ({ToolshedCommandImplementor.GetFriendlyName(cmd.PipedType)})> -> ");
            }

            if (cmd.Bundle.Inverted)
                builder.Append("not ");

            builder.Append($"{name}");
            foreach (var (argName, argType, _) in cmd.Method.Args)
            {
                builder.Append($" <{argName} ({ToolshedCommandImplementor.GetFriendlyName(argType)})>");
            }

            builder.AppendLine();
            var piped = cmd.PipedType?.PrettyName() ?? "[none]";
            var returned = cmd.ReturnType?.PrettyName() ?? "[none]";
            builder.AppendLine($"{piped} -> {returned}");
            builder.AppendLine();
        }

        ctx.WriteLine(builder.ToString());
    }
}
