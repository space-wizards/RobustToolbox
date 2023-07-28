using System.Collections;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
internal sealed class IsEmptyCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool IsEmpty<T>([PipedArgument] T? input)
    {
        if (input is null)
            return true; // Null is empty for all we care.

        if (input is IEnumerable @enum)
        {
            return !@enum.Cast<object?>().Any();
        }

        return false; // Not a collection, cannot be empty.
    }
}
