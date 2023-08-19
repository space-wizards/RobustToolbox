using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class BIByteCountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Operation<T>([PipedArgument] T x)
        where T : IBinaryInteger<T>
    {
        return x.GetByteCount();
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<int> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IBinaryInteger<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class ShortestBitLengthCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Operation<T>([PipedArgument] T x)
        where T : IBinaryInteger<T>
    {
        return x.GetShortestBitLength();
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<int> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IBinaryInteger<T>
        => x.Select(Operation);
}


[ToolshedCommand]
public sealed class CountLeadZerosCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IBinaryInteger<T>
    {
        return T.LeadingZeroCount(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IBinaryInteger<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class CountTrailingZerosCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IBinaryInteger<T>
    {
        return T.TrailingZeroCount(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IBinaryInteger<T>
        => x.Select(Operation);
}
