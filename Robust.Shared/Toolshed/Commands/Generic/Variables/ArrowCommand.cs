using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic.Variables ;

[ToolshedCommand(Name = "=>")]
public sealed class ArrowCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Arrow<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] T input,
            [CommandArgument] ValueRef<T> @ref
        )
    {
        @ref.Set(ctx, input);
        return input;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public List<T> Arrow<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<T> input,
            [CommandArgument] ValueRef<List<T>> @ref
        )
    {
        var list = input.ToList();
        @ref.Set(ctx, list);
        return list;
    }
}
