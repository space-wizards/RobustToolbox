using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class SqrtCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IRootFunctions<T>
    {
        return T.Sqrt(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IRootFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class CbrtCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x)
        where T : IRootFunctions<T>
    {
        return T.Cbrt(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x)
        where T : IRootFunctions<T>
        => x.Select(Operation);
}

[ToolshedCommand]
public sealed class RootCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>(
        [PipedArgument] T x,
        int y
    )
        where T : IRootFunctions<T>
    {
        return T.RootN(x, y);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<int> y) where T : IRootFunctions<T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}

[ToolshedCommand]
public sealed class HypotCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x, T y) where T : IRootFunctions<T>
    {
        return T.Hypot(x, y);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y) where T : IRootFunctions<T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}
