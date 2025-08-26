namespace Robust.Shared.Toolshed.Commands.Generic;

// TODO TOOLSHED
// Combine with other "is...." commands into is:empty
[ToolshedCommand(Name = "isnull")]
public sealed class IsNullCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool IsNull([PipedArgument] object? input, [CommandInverted] bool inverted) => input is null ^ inverted;
}
