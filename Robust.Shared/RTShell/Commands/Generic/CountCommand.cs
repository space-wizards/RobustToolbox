using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.RTShell.Commands.Generic;

[ConsoleCommand]
internal sealed class CountCommand : ConsoleCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Count<T>([PipedArgument] IEnumerable<T> enumerable)
    {
        return enumerable.Count();
    }
}
