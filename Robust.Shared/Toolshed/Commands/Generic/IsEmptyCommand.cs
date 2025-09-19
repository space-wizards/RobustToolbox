using System.Collections;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic;

// TODO TOOLSHED
// Combine with other "is...." commands into is:empty
[ToolshedCommand(Name = "isempty")]
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
