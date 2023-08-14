using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic.ListGeneration;

[ToolshedCommand]
public sealed class ToCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> To<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] T start,
            [CommandArgument] ValueRef<T> end
        )
        where T : INumber<T>
        => Enumerable.Range(int.CreateTruncating(start), int.CreateTruncating(end.Evaluate(ctx)!)).Select(T.CreateTruncating);
}
