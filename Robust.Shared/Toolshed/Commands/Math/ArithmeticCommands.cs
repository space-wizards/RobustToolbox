using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

#region Core arithmetic
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

    [CommandImplementation]
    public Vector2 Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] Vector2 x,
        [CommandArgument] ValueRef<Vector2> y
    )
    {
        var yVal = y.Evaluate(ctx);
        return x + yVal;
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<Vector2> x,
        [CommandArgument] ValueRef<IEnumerable<Vector2>> y
    )
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<Vector2>(right));
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

    [CommandImplementation]
    public IEnumerable<Vector2> Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<Vector2> x,
        [CommandArgument] ValueRef<Vector2> y
    )
    {
        var yVal = y.Evaluate(ctx);
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

    [CommandImplementation]
    public Vector2 Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] Vector2 x,
        [CommandArgument] ValueRef<Vector2> y
    )
    {
        var yVal = y.Evaluate(ctx);
        return x - yVal;
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<Vector2> x,
        [CommandArgument] ValueRef<IEnumerable<Vector2>> y
    )
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<Vector2>(right));
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

    [CommandImplementation]
    public IEnumerable<Vector2> Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<Vector2> x,
        [CommandArgument] ValueRef<Vector2> y
    )
    {
        var yVal = y.Evaluate(ctx);
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

    [CommandImplementation]
    public Vector2 Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] Vector2 x,
        [CommandArgument] ValueRef<Vector2> y
    )
    {
        var yVal = y.Evaluate(ctx);
        return x * yVal;
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<Vector2> x,
        [CommandArgument] ValueRef<IEnumerable<Vector2>> y
    )
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<Vector2>(right));
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

    [CommandImplementation]
    public IEnumerable<Vector2> Operation(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<Vector2> x,
        [CommandArgument] ValueRef<Vector2> y
    )
    {
        var yVal = y.Evaluate(ctx);
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
        where T : INumberBase<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;

        if (T.IsZero(yVal))
            return T.Zero;

        return x / yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : INumberBase<T>
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
        where T : INumberBase<T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;

        if (T.IsZero(yVal))
            return x.Select(_ => T.Zero);

        return x.Select(i => i / yVal);
    }
}

[ToolshedCommand(Name = "%")]
public sealed class ModulusCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IModulusOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx)!;
        return x % yVal;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : IModulusOperators<T, T, T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

[ToolshedCommand(Name = "%/")]
public sealed class ModVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<T> y
    )
        where T : IModulusOperators<T, T, T>
    {
        var yVal = y.Evaluate(ctx);
        if (yVal is null)
            return x;
        return x.Select(i => i % yVal);
    }
}
#endregion

[ToolshedCommand]
public sealed class MinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x,
        [CommandArgument] ValueRef<T> y
    )
        where T : INumberBase<T>
    {
        var yVal = y.Evaluate(ctx)!;
        return T.MinMagnitude(x, yVal);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : INumberBase<T>
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
        where T : INumberBase<T>
    {
        var yVal = y.Evaluate(ctx)!;
        return T.MaxMagnitude(x, yVal);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x,
        [CommandArgument] ValueRef<IEnumerable<T>> y
    )
        where T : INumberBase<T>
        => x.Zip(y.Evaluate(ctx)!).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(ctx, left, new ValueRef<T>(right));
        });
}

#region Bitwise
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

[ToolshedCommand(Name = "&~")]
public sealed class BitAndNotCommand : ToolshedCommand
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
        return x & ~yVal;
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

[ToolshedCommand(Name = "|~")]
public sealed class BitOrNotCommand : ToolshedCommand
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
        return x | ~yVal;
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

[ToolshedCommand(Name = "^~")]
public sealed class BitXnorCommand : ToolshedCommand
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
        return x ^ ~yVal;
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

[ToolshedCommand(Name = "~")]
public sealed class BitNotCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] T x
    )
        where T : IBitwiseOperators<T, T, T>
    {
        return ~x;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> x
    )
        where T : IBitwiseOperators<T, T, T>
        => x.Select(v => Operation<T>(ctx, v));
}

#endregion

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

[ToolshedCommand]
public sealed class AbsCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.Abs(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}
