using System.Collections.Generic;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class IterateCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T>? Iterate<T>(
            IInvocationContext ctx,
            [PipedArgument] T value,
            Block<T, T> block,
            int times
        )
    {
        for (var i = 0; i < times; i++)
        {
            if (block.Invoke(value, ctx) is not { } v)
                break;

            if (ctx.HasErrors)
                break;

            value = v;
            yield return value;
        }
    }
}
