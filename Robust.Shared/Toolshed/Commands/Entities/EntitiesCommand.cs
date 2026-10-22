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
        public EntityUid[]? _arr;

        public IEnumerator<EntityUid> GetEnumerator()
        {
            _arr ??= EntMan.GetEntities().ToArray();
            return ((IEnumerable<EntityUid>)_arr).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            _arr ??= EntMan.GetEntities().ToArray();
            return _arr.GetEnumerator();
        }
    }
}
