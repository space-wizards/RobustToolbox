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
            builder.AppendLine();
            var name = cmd.Implementor.FullName;
            builder.AppendLine($"{name} - {cmd.Implementor.Description()}");

            var piped = cmd.PipedType?.PrettyName() ?? "[none]";
            builder.AppendLine($"Pipe input: {piped}");
            builder.AppendLine($"Pipe output: {cmd.ReturnType.PrettyName()}");

            builder.Append($"Signature:\n  ");

            if (cmd.PipedType != null)
            {
                var pipeArg = cmd.Method.Base.PipeArg;
                DebugTools.AssertNotNull(pipeArg);

                var locKey = $"command-arg-sig-{cmd.Implementor.LocName}-{pipeArg?.Name}";
                if (Loc.TryGetString(locKey, out var msg))
                {
                    builder.Append(msg);
                    builder.Append(" → ");
                }
                else
                {
                    builder.Append($"<{pipeArg?.Name}> → "); // No type information, as that is already given above.
                }
            }

            if (cmd.Bundle.Inverted)
                builder.Append("not ");

            cmd.Implementor.AddMethodSignature(builder, cmd.Method.Args, cmd.Bundle.TypeArguments);
            builder.AppendLine();
        }

        ctx.WriteLine(builder.ToString().TrimEnd());
    }
}
