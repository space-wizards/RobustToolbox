using System;
using System.Text;
using Microsoft.Extensions.Primitives;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
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
            builder.AppendLine();
            var name = cmd.Implementor.FullName;
            builder.AppendLine($"{name} - {cmd.Implementor.Description()}");

            if (cmd.PipedType != null)
            {
                var pipeArg = cmd.Method.Base.PipeArg;
                DebugTools.AssertNotNull(pipeArg);
                builder.Append($"<{pipeArg?.Name} ({cmd.PipedType.PrettyName()})> -> ");
            }

            if (cmd.Bundle.Inverted)
                builder.Append("not ");

            cmd.Implementor.AddMethodSignature(builder, cmd.Method.Args, cmd.Bundle.TypeArguments);

            builder.AppendLine();
            var piped = cmd.PipedType?.PrettyName() ?? "[none]";
            var returned = cmd.ReturnType?.PrettyName() ?? "[none]";
            builder.AppendLine($"{piped} -> {returned}");
        }

        ctx.WriteLine(builder.ToString().TrimEnd());
    }
}
