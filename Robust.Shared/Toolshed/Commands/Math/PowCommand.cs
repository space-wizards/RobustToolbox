using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class PowCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IPowerFunctions<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return T.Pow(x, yVal);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IPowerFunctions<T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}
