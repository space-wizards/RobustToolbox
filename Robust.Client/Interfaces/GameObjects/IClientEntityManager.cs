using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace Robust.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        List<EntityUid> ApplyEntityStates(List<EntityState> curEntStates, IEnumerable<EntityUid> deletions,
            List<EntityState> nextEntStates);
    }
}
