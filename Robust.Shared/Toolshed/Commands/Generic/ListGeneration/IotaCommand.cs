using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Generic.ListGeneration;

[ToolshedCommand]
public sealed class IotaCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Iota<T>([PipedArgument] T count)
        where T : INumber<T>
        => Enumerable.Range(1, int.CreateTruncating(count)).Select(T.CreateTruncating);
}
