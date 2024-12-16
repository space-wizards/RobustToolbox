using System;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand(Name = ">")]
public sealed class GreaterThanCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>([PipedArgument] T x, T y) where T : INumber<T>
    {
        return x > y;
    }
}

[ToolshedCommand(Name = "<")]
public sealed class LessThanCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>([PipedArgument] T x, T y)
        where T : IComparisonOperators<T, T, bool>
    {
        return x > y;
    }
}

[ToolshedCommand(Name = ">=")]
public sealed class GreaterThanOrEqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>([PipedArgument] T x, T y)
        where T : INumber<T>
    {
        return x >= y;
    }
}

[ToolshedCommand(Name = "<=")]
public sealed class LessThanOrEqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>([PipedArgument] T x, T y)
        where T : IComparisonOperators<T, T, bool>
    {
        return x <= y;
    }
}

[ToolshedCommand(Name = "==")]
public sealed class EqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>([PipedArgument] T x, T y)
        where T : IEquatable<T>
    {
        return x.Equals(y);
    }
}

[ToolshedCommand(Name = "!=")]
public sealed class NotEqualCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Comparison<T>([PipedArgument] T x, T y)
        where T : IEquatable<T>
    {
        return !x.Equals(y);
    }
}
