using System.Collections;
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
        return new AllEntityEnumerator(EntityManager);
    }

    public sealed class AllEntityEnumerator(IEntityManager entMan) : IEnumerable<EntityUid>
    {
        public IEntityManager EntMan { get; } = entMan;

        // We create an array as chained commands might modify it.
        public EntityUid[]? Entities;

        public IEnumerator<EntityUid> GetEnumerator()
        {
            Entities ??= EntMan.GetEntities().ToArray();
            return ((IEnumerable<EntityUid>)Entities).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Entities ??= EntMan.GetEntities().ToArray();
            return Entities.GetEnumerator();
        }
    }
}
