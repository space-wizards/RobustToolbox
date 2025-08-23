using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Player;

namespace Robust.Shared.Toolshed.Commands.Math;

// TODO TOOLSHED
// Turn this into subcommands?
// i.e, is:complex

[ToolshedCommand(Name = "iscanonical")]
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

[ToolshedCommand(Name = "iscomplex")]
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

[ToolshedCommand(Name = "iseven")]
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

[ToolshedCommand(Name = "isodd")]
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

[ToolshedCommand(Name = "isfinite")]
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

[ToolshedCommand(Name = "isimaginary")]
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

[ToolshedCommand(Name = "isinfinite")]
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

[ToolshedCommand(Name = "isinteger")]
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

[ToolshedCommand(Name = "isnan")]
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

[ToolshedCommand(Name = "isnegative")]
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

[ToolshedCommand(Name = "ispositive")]
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

[ToolshedCommand(Name = "isreal")]
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

[ToolshedCommand(Name = "issubnormal")]
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

[ToolshedCommand(Name = "iszero")]
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
