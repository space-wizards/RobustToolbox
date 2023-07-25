using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

[RtShellCommand]
internal sealed class CountCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Count<T>([PipedArgument] IEnumerable<T> enumerable)
    {
        return enumerable.Count();
    }
}
