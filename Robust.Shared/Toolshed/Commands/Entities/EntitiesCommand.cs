using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class EntitiesCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Entities()
    {
        // NOTE: Makes a copy due to the fact chained on commands might modify this list.
        return EntityManager.GetEntities().ToHashSet();
    }
}
