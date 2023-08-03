using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class VarsCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Vars([CommandInvocationContext] IInvocationContext ctx)
    {
        ctx.WriteLine(Toolshed.PrettyPrintType(ctx.GetVars().Select(x => $"{x} = {ctx.ReadVar(x)}")));
    }
}
