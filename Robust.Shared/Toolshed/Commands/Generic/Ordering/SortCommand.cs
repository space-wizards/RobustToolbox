using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic.Ordering;

[ToolshedCommand]
public sealed class SortCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Sort<T>([PipedArgument] IEnumerable<T> input) where T : IComparable<T>
        => input.Order();
}
