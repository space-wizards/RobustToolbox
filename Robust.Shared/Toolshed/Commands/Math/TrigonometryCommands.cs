using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

#region Sine
[ToolshedCommand]
public sealed class SinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.Sin(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "sinpi")]
public sealed class SinPiCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.SinPi(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "asin")]
public sealed class ASinCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.Asin(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "asinpi")]
public sealed class ASinPiCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.AsinPi(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}
#endregion
#region Cosine
[ToolshedCommand]
public sealed class CosCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.Cos(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "cospi")]
public sealed class CosPiCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.CosPi(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "acos")]
public sealed class ACosCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.Acos(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "acospi")]
public sealed class ACosPiCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.AcosPi(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}
#endregion
#region Tangent
[ToolshedCommand]
public sealed class TanCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.Tan(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "tanpi")]
public sealed class TanPiCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.TanPi(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "atan")]
public sealed class ATanCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.Atan(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand(Name = "atanpi")]
public sealed class ATanPiCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : ITrigonometricFunctions<T>
    {
        return T.AtanPi(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : ITrigonometricFunctions<T>
        => x.Select(Operation);
}
#endregion
