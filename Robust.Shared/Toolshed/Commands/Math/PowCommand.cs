using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class PowCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Operation<T>([PipedArgument] T x,T y) where T : IPowerFunctions<T>
    {
        return T.Pow(x, y);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Operation<T>([PipedArgument] IEnumerable<T> x, IEnumerable<T> y) where T : IPowerFunctions<T>
        => x.Zip(y).Select(inp =>
        {
            var (left, right) = inp;
            return Operation(left, right);
        });
}
