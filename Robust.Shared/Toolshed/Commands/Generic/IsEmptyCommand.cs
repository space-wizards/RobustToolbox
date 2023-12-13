using System.Collections;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class IsEmptyCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public bool IsEmpty<T>([PipedArgument] T? input, [CommandInverted] bool inverted)
    {
        if (input is null)
            return true ^ inverted; // Null is empty for all we care.

        if (input is IEnumerable @enum)
        {
            return !@enum.Cast<object?>().Any() ^ inverted;
        }

        return false ^ inverted; // Not a collection, cannot be empty.
    }
}
