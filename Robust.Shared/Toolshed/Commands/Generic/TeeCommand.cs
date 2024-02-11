using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand, MapLikeCommand]
public sealed class TeeCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut> Tee<TOut, TIn>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<TIn> value,
            [CommandArgument] Block<TIn, TOut> block
        )
    {
        return value.Select(x =>
        {
            block.Invoke(x, ctx);
            return x;
        }).Where(x => x != null).Cast<TOut>();
    }
}
