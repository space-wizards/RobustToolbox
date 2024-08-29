namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class IsNullCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool IsNull([PipedArgument] object? input, [CommandInverted] bool inverted) => input is null ^ inverted;
}
