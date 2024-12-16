using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
internal sealed class ContainsCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Contains<T>([PipedArgument] IEnumerable<T> input, T value, [CommandInverted] bool inverted)
    {
        return inverted ^ input.Contains(value);
    }
}
