using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class CountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Count<T>([PipedArgument] IEnumerable<T> enumerable)
    {
        return enumerable.Count();
    }
}
