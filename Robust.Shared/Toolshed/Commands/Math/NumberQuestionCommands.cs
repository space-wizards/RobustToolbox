using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Player;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class IsCanonicalCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsCanonical(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsComplexCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsComplexNumber(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsEvenCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsEvenInteger(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsOddCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsOddInteger(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsFiniteCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsFinite(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsImaginaryCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsImaginaryNumber(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);

    // everyone on the internet is imaginary except you.
    [CommandImplementation]
    public bool Operation(IInvocationContext ctx, [PipedArgument] ICommonSession x)
    {
        return ctx.Session != x;
    }
}

[ToolshedCommand]
public sealed class IsInfiniteCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsInfinity(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsIntegerCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsInteger(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsNaNCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsNaN(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsNegativeCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsNegative(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsPositiveCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsPositive(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsRealCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsRealNumber(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);

    // nobody on the internet is real except you.
    [CommandImplementation]
    public bool Operation(IInvocationContext ctx, [PipedArgument] ICommonSession x)
    {
        return ctx.Session == x;
    }
}

[ToolshedCommand]
public sealed class IsSubnormalCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsSubnormal(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class IsZeroCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Operation<T>([PipedArgument] T x)
        where T : INumberBase<T>
    {
        return T.IsZero(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<bool> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : INumberBase<T>
        => x.Select(Operation);
}
