using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class WhereCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Where<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<T> input,
            [CommandArgument] Block<T, bool> check
        )
    {
        foreach (var i in input)
        {
            var res = check.Invoke(i, ctx);

            if (ctx.GetErrors().Any())
                yield break;

            if (res)
                yield return i;
        }
    }
}
