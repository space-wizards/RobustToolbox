using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class PickCommand : ToolshedCommand
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Pick<T>([PipedArgument] IEnumerable<T> input)
    {
        return _random.Pick(input.ToArray());
    }
}
