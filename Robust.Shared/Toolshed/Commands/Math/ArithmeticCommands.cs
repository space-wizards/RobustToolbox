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
}

[ToolshedCommand]
public sealed class NegCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x
    )
        where T : IUnaryNegationOperators<T, T>
    {
        return -x;
    }
}
