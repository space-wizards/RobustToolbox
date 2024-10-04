using System.Collections.Generic;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class WhereCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Where<T>(
            IInvocationContext ctx,
            [PipedArgument] IEnumerable<T> input,
            Block<T, bool> check
        )
    {
        foreach (var i in input)
        {
            var res = check.Invoke(i, ctx);

            if (ctx.HasErrors)
                yield break;

            if (res)
                yield return i;
        }
    }
}
