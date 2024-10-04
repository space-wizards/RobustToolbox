using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Generic.ListGeneration;

[ToolshedCommand]
public sealed class ToCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> To<T>([PipedArgument] T start, T end) where T : INumber<T>
        => Enumerable.Range(int.CreateTruncating(start), 1 + int.CreateTruncating(end - start)).Select(T.CreateTruncating);
}
