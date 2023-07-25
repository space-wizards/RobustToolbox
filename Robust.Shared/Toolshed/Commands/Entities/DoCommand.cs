using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class DoCommand : ToolshedCommand
{
    public void Do(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> entities,
        [CommandArgument] string command)
    {

    }
}
