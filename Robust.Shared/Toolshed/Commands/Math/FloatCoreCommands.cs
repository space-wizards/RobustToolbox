using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

#region Constants
[ToolshedCommand]
public sealed class FPiCommand : ToolshedCommand
{
    [CommandImplementation]
    public float Const() => float.Pi;
}

[ToolshedCommand]
public sealed class FECommand : ToolshedCommand
{
    [CommandImplementation]
    public float Const() => float.E;
}

[ToolshedCommand]
public sealed class FTauCommand : ToolshedCommand
{
    [CommandImplementation]
    public float Const() => float.Tau;
}

[ToolshedCommand]
public sealed class FEpsilonCommand : ToolshedCommand
{
    [CommandImplementation]
    public float Const() => float.Epsilon;
}

[ToolshedCommand]
public sealed class DPiCommand : ToolshedCommand
{
    [CommandImplementation]
    public double Const() => double.Pi;
}

[ToolshedCommand]
public sealed class DECommand : ToolshedCommand
{
    [CommandImplementation]
    public double Const() => double.E;
}

[ToolshedCommand]
public sealed class DTauCommand : ToolshedCommand
{
    [CommandImplementation]
    public double Const() => double.Tau;
}

[ToolshedCommand]
public sealed class DEpsilonCommand : ToolshedCommand
{
    [CommandImplementation]
    public double Const() => double.Epsilon;
}

[ToolshedCommand]
public sealed class HPiCommand : ToolshedCommand
{
    [CommandImplementation]
    public Half Const() => Half.Pi;
}

[ToolshedCommand]
public sealed class HECommand : ToolshedCommand
{
    [CommandImplementation]
    public Half Const() => Half.E;
}

[ToolshedCommand]
public sealed class HTauCommand : ToolshedCommand
{
    [CommandImplementation]
    public Half Const() => Half.Tau;
}

[ToolshedCommand]
public sealed class HEpsilonCommand : ToolshedCommand
{
    [CommandImplementation]
    public Half Const() => Half.Epsilon;
}
#endregion

#region Rounding
[ToolshedCommand]
public sealed class FloorCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return T.Floor(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class CeilCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return T.Ceiling(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class RoundCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return T.Round(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class TruncCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return T.Truncate(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class Round2FracCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, int frac)
        where T : IFloatingPoint<T>
    {
        return T.Round(x, frac);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, int frac)
        where T : IFloatingPoint<T>
        => x.Select(v => Operation(v, frac));
}
#endregion

#region Bitfiddling
[ToolshedCommand]
public sealed class ExponentByteCountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return x.GetExponentByteCount();
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<int> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class SignificandByteCountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return x.GetSignificandByteCount();
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<int> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class SignificandBitCountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Operation<T>([PipedArgument] T x)
        where T : IFloatingPoint<T>
    {
        return x.GetSignificandBitLength();
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<int> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class ExponentShortestBitCountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Operation<T>([PipedArgument] T x) where T : IFloatingPoint<T>
    {
        return x.GetExponentShortestBitLength();
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<int> Operation<T>([PipedArgument] IEnumerable<T> x) where T : IFloatingPoint<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class StepNextCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x) where T : IFloatingPointIeee754<T>
        => T.BitIncrement(x);

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x) where T : IFloatingPointIeee754<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class StepPrevCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x) where T : IFloatingPointIeee754<T>
        => T.BitDecrement(x);

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x) where T : IFloatingPointIeee754<T>
        => x.Select(Operation);
}
#endregion

