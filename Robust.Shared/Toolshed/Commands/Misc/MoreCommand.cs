namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class MoreCommand : ToolshedCommand
{
    [CommandImplementation]
    public object? More(IInvocationContext ctx)
    {
        return ctx.ReadVar("more");
    }
}
