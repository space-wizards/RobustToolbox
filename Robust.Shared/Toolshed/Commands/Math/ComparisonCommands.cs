using System;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand(Name = ">")]
internal sealed class GreaterThanCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] T x,
            [CommandArgument] ValueRef<T> y
        )
        where T : INumber<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return false;
        return x > yVal;
    }
}

[ToolshedCommand(Name = "<")]
internal sealed class LessThanCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IComparisonOperators<T, T, bool>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return false;
        return x > yVal;
    }
}

[ToolshedCommand(Name = ">=")]
internal sealed class GreaterThanOrEqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : INumber<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return false;
        return x >= yVal;
    }
}

[ToolshedCommand(Name = "<=")]
internal sealed class LessThanOrEqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IComparisonOperators<T, T, bool>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return false;
        return x <= yVal;
    }
}

[ToolshedCommand(Name = "==")]
internal sealed class EqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IEquatable<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return false;
        return x.Equals(yVal);
    }
}

[ToolshedCommand(Name = "!=")]
internal sealed class NotEqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IEquatable<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return false;
        return !x.Equals(yVal);
    }
}
