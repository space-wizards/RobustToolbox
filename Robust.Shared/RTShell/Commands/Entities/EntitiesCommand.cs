using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.RTShell.Commands.Entities;

[RtShellCommand]
internal sealed class EntitiesCommand : RtShellCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Entities()
    {
        // NOTE: Makes a copy due to the fact chained on commands might modify this list.
        return EntityManager.GetEntities().ToHashSet();
    }
}
