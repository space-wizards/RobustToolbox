using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

#region Core arithmetic
[ToolshedCommand(Name = "+")]
public sealed class AddCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IAdditionOperators<T, T, T>
    {
        return x + y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IAdditionOperators<T, T, T>
        => x.Zip(y)
            .Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });

    [CommandImplementation]
    public Vector2 Operation([PipedArgument] Vector2 x, Vector2 y)
    {
        return x + y;
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation([PipedArgument] IEnumerable<Vector2> x, IEnumerable<Vector2> y)
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "+/")]
public sealed class AddVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, T y)
        where T : IAdditionOperators<T, T, T>
    {
        return x.Select(i => i + y);
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation([PipedArgument] IEnumerable<Vector2> x, Vector2 y)
    {
        return x.Select(i => i + y);
    }
}

[ToolshedCommand(Name = "-")]
public sealed class SubtractCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : ISubtractionOperators<T, T, T>
    {
        return x - y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : ISubtractionOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });

    [CommandImplementation]
    public Vector2 Operation([PipedArgument] Vector2 x, Vector2 y)
    {
        return x - y;
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation([PipedArgument] IEnumerable<Vector2> x, IEnumerable<Vector2> y)
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "-/")]
public sealed class SubVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, T y)
        where T : ISubtractionOperators<T, T, T>
    {
        return x.Select(i => i - y);
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation([PipedArgument] IEnumerable<Vector2> x, Vector2 y)
    {
        return x.Select(i => i - y);
    }
}

[ToolshedCommand(Name = "*")]
public sealed class MultiplyCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IMultiplyOperators<T, T, T>
    {
        return x * y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IMultiplyOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });

    [CommandImplementation]
    public Vector2 Operation([PipedArgument] Vector2 x, Vector2 y)
    {
        return x * y;
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation([PipedArgument] IEnumerable<Vector2> x, IEnumerable<Vector2> y)
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "*/")]
public sealed class MulVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, T y)
        where T : IMultiplyOperators<T, T, T>
    {
        return x.Select(i => i * y);
    }

    [CommandImplementation]
    public IEnumerable<Vector2> Operation([PipedArgument] IEnumerable<Vector2> x, Vector2 y)
    {
        return x.Select(i => i * y);
    }
}

[ToolshedCommand(Name = "/")]
public sealed class DivideCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : INumberBase<T>
    {
        if (T.IsZero(y))
            return T.Zero;

        return x / y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y) where T : INumberBase<T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "//")]
public sealed class DivVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, T y) where T : INumberBase<T>
    {
        if (T.IsZero(y))
            return x.Select(_ => T.Zero);

        return x.Select(i => i / y);
    }
}

[ToolshedCommand(Name = "%")]
public sealed class ModulusCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IModulusOperators<T, T, T>
    {
        return x % y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IModulusOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "%/")]
public sealed class ModVecCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, T y) where T : IModulusOperators<T, T, T>
    {
        return x.Select(i => i % y);
    }
}
#endregion

[ToolshedCommand]
public sealed class MinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : INumberBase<T>
    {
        return T.MinMagnitude(x, y);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y) where T : INumberBase<T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand]
public sealed class MaxCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : INumberBase<T>
    {
        return T.MaxMagnitude(x, y);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y) where T : INumberBase<T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

#region Bitwise
[ToolshedCommand(Name = "&")]
public sealed class BitAndCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IBitwiseOperators<T, T, T>
    {
        return x & y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "&~")]
public sealed class BitAndNotCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IBitwiseOperators<T, T, T>
    {
        return x & ~y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand]
public sealed class BitOrCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IBitwiseOperators<T, T, T>
    {
        return x | y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand]
public sealed class BitOrNotCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IBitwiseOperators<T, T, T>
    {
        return x | ~y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "^")]
public sealed class BitXorCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IBitwiseOperators<T, T, T>
    {
        return x ^ y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "^~")]
public sealed class BitXnorCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IBitwiseOperators<T, T, T>
    {
        return x ^ ~y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y)
        where T : IBitwiseOperators<T, T, T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand(Name = "~")]
public sealed class BitNotCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x) where T : IBitwiseOperators<T, T, T>
    {
        return ~x;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x) where T : IBitwiseOperators<T, T, T>
        => x.Select(Operation);
}

#endregion

[ToolshedCommand]
public sealed class NegCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x) where T : IUnaryNegationOperators<T, T>
    {
        return -x;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x) where T : IUnaryNegationOperators<T, T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class AbsCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x) where T : INumberBase<T>
    {
        return T.Abs(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x) where T : INumberBase<T>
        => x.Select(Operation);
}
