using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

internal sealed class FirstCommand : ToolshedCommand
{
    [CommandImplementation()]
    public T First<T>([PipedArgument] IEnumerable<T> input) => input.First();
}
