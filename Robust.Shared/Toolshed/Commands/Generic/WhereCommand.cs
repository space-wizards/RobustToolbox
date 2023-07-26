using System.Collections.Generic;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
internal sealed class WhereCommand : ToolshedCommand
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

            if (res)
                yield return i;
        }
    }
}
