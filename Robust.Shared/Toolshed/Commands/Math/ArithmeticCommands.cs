using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand(Name = "+")]
public sealed class AddCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IAdditionOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x + yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IAdditionOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "+/")]
public sealed class AddVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IAdditionOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x.Select(i => i + yVal);
    }
}

[ToolshedCommand(Name = "-")]
public sealed class SubtractCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : ISubtractionOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x - yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : ISubtractionOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "-/")]
public sealed class SubVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<T> y
    )
        where T : ISubtractionOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x.Select(i => i - yVal);
    }
}

[ToolshedCommand(Name = "*")]
public sealed class MultiplyCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IMultiplyOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx)!;
        return x * yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IMultiplyOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "*/")]
public sealed class MulVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IMultiplyOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x.Select(i => i * yVal);
    }
}

[ToolshedCommand(Name = "/")]
public sealed class DivideCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IDivisionOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx)!;
        return x / yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IDivisionOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "//")]
public sealed class DivVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IDivisionOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x.Select(i => i / yVal);
    }
}


[ToolshedCommand]
public sealed class MinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IComparisonOperators<T, T, bool>
    {
        var yVal = y.Evaluate(ctx)!;
        return x > yVal ? yVal : x;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IComparisonOperators<T, T, bool>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand]
public sealed class MaxCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IComparisonOperators<T, T, bool>
    {
        var yVal = y.Evaluate(ctx)!;
        return x > yVal ? x : yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IComparisonOperators<T, T, bool>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "&")]
public sealed class BitAndCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IBitwiseOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx)!;
        return x & yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "|")]
public sealed class BitOrCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IBitwiseOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx)!;
        return x | yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "^")]
public sealed class BitXorCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IBitwiseOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx)!;
        return x ^ yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand]
public sealed class NegCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IUnaryNegationOperators<T, T>
    {
        return -x;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IUnaryNegationOperators<T, T>
        => x.Select(Operation);
}
