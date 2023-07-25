using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class WithCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> With([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] ComponentType ty, [CommandInverted] bool inverted)
    {
        return input.Where(x => EntityManager.HasComponent(x, ty.Ty) ^ inverted);
    }
}
