using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.RTShell.Commands.Entities;

[ConsoleCommand]
public sealed class EntitiesCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Entities()
    {
        return EntityManager.GetEntities();
    }
}
