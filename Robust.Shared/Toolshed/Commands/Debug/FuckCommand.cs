using System;

namespace Robust.Shared.Toolshed.Commands.Debug;

[ToolshedCommand]
public sealed class FuckCommand : ToolshedCommand
{
    [CommandImplementation]
    public object? Fuck([PipedArgument] object? value)
    {
        throw new Exception("fuck!");
    }

    [CommandImplementation]
    public object? Fuck()
    {
        throw new Exception("fuck!");
    }
}
