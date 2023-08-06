namespace Robust.Shared.Toolshed.Commands.Misc;

public sealed class MoreCommand
{
    [CommandImplementation]
    public object? More([CommandInvocationContext] IInvocationContext ctx)
    {
        return ctx.ReadVar("more");
    }
}
