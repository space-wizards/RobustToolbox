using System;

namespace Robust.Shared.Toolshed.Commands.Debug;

[RtShellCommand]
internal sealed class FuckCommand : ToolshedCommand
{
    [CommandImplementation]
    public object? Fuck([PipedArgument] object? value)
    {
        throw new Exception("fuck!");
    }
}
