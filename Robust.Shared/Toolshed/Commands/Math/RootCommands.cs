using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class SqrtCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IRootFunctions<T>
    {
        return T.Sqrt(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IRootFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class CbrtCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IRootFunctions<T>
    {
        return T.Cbrt(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IRootFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class RootCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<int> y
    )
        where T : IRootFunctions<T>
    {
        var yVal = y.Evaluate(ctx);
        return T.RootN(x, yVal);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<int>> y
    )
        where T : IRootFunctions<T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<int>(right));
        });
}

[ToolshedCommand]
public sealed class HypotCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IRootFunctions<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return default!;
        return T.Hypot(x, yVal);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IRootFunctions<T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}
