using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Generic.ListGeneration;

[ToolshedCommand(Name = "rep")]
public sealed class RepeatCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Repeat<T>([PipedArgument] T value, int amount)
        => Enumerable.Repeat(value, amount);
}
