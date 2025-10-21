using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic.Variables;

[ToolshedCommand]
public sealed class VarsCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Vars(IInvocationContext ctx)
    {
        ctx.WriteLine(Toolshed.PrettyPrintType(ctx.GetVars().Select(x => $"{x} = {Toolshed.PrettyPrintType(ctx.ReadVar(x), out _)}"), out _));
    }
}
