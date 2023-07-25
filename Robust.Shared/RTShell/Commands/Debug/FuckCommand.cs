using System;

namespace Robust.Shared.RTShell.Commands.Debug;

[RtShellCommand]
internal sealed class FuckCommand : RtShellCommand
{
    [CommandImplementation]
    public object? Fuck([PipedArgument] object? value)
    {
        throw new Exception("fuck!");
    }
}
