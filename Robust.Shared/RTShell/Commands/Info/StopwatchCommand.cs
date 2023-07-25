using Robust.Shared.Maths;
using Robust.Shared.RTShell.Syntax;
using Robust.Shared.Timing;

namespace Robust.Shared.RTShell.Commands.Info;

[RtShellCommand]
internal sealed class StopwatchCommand : RtShellCommand
{
    [CommandImplementation]
    public object? Stopwatch([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] CommandRun expr)
    {
        var watch = new Stopwatch();
        watch.Start();
        var result = expr.Invoke(null, ctx);
        ctx.WriteMarkup($"Ran expression in [color={Color.Aqua.ToHex()}]{watch.Elapsed:g}[/color]");
        return result;
    }
}
