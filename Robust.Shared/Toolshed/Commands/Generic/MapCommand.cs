using System;
using System.Collections.Generic;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class MapCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(MapBlockOutputParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut> Map<TOut, TIn>(
            IInvocationContext ctx,
            [PipedArgument] IEnumerable<TIn> value,
            Block<TIn, TOut> block
        )
    {
        foreach (var x in value)
        {
            if (block.Invoke(x, ctx) is { } result)
                yield return result;

            if (ctx.HasErrors)
                break;
        }
    }
}
