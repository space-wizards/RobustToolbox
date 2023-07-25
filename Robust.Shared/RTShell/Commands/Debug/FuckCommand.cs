using System;

namespace Robust.Shared.RTShell.Commands.Debug;

[ConsoleCommand]
internal sealed class FuckCommand : ConsoleCommand
{
    [CommandImplementation]
    public object? Fuck([PipedArgument] object? value)
    {
        throw new Exception("fuck!");
    }
}
