using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class IterateCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T>? Iterate<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] T value,
            [CommandArgument] Block<T, T> block,
            [CommandArgument] ValueRef<int> times
        )
    {
        var iCap = times.Evaluate(ctx);

        for (var i = 0; i < iCap; i++)
        {
            if (block.Invoke(value, ctx) is not { } v)
                break;
            value = v;
            yield return value;
        }
    }
}
