using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        /// <returns>The list of new entities created.</returns>
        List<EntityUid> ApplyEntityStates(EntityState[]? curEntStates, IEnumerable<EntityUid>? deletions,
            EntityState[]? nextEntStates);
    }
}
