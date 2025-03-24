using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class JoinCommand : ToolshedCommand
{
    [CommandImplementation]
    public string Join(
        [PipedArgument] string x,
        string y
    )
    {
        return x + y;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Join<T>(
            [PipedArgument] IEnumerable<T> x,
            IEnumerable<T> y
        )
    {
        return x.Concat(y);
    }
}

[ToolshedCommand]
public sealed class AppendCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<T> Append<T>(
        [PipedArgument] IEnumerable<T> x,
        T y
    )
    {
        return x.Append(y);
    }
}
