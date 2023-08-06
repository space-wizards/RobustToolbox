using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
internal sealed class AnyCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool Any<T>([PipedArgument] IEnumerable<T> input) => input.Any();
}
