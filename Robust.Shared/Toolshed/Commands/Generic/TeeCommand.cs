using System;
using System.Collections.Generic;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class TeeCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(MapBlockOutputParser)];
    public override Type[] TypeParameterParsers => _parsers;

    // Take in some input, use it to evaluate some block, and then just keep passing along the input, disregarding the
    // output of the block. I.e., this behaves like the standard tee tee command, where the block is the "file".
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TIn> Tee<TOut, TIn>(
            IInvocationContext ctx,
            [PipedArgument] IEnumerable<TIn> value,
            Block<TIn, TOut> block
        )
    {
        foreach (var x in value)
        {
            block.Invoke(x, ctx);
            if (ctx.HasErrors)
                yield break;
            yield return x;
        }
    }
}
