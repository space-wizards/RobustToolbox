using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class StopwatchCommand : ToolshedCommand
{
    [CommandImplementation]
    public object? Stopwatch(IInvocationContext ctx, CommandRun expr)
    {
        var watch = new Stopwatch();
        watch.Start();
        var result = expr.Invoke(null, ctx);
        ctx.WriteMarkup($"Ran expression in [color={Color.Aqua.ToHex()}]{watch.Elapsed:g}[/color]");
        return result;
    }
}
