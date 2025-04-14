using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic.Ordering;

[ToolshedCommand(Name = "sortdown")]
public sealed class SortDownCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Sort<T>([PipedArgument] IEnumerable<T> input) where T : IComparable<T>
        => input.OrderDescending();
}
