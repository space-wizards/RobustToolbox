using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace Robust.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        EntityUid GetClientId(EntityUid serverId);

        bool TryGetClientId(EntityUid serverId, out EntityUid clientId);

        EntityUid GetServerId(EntityUid clientId);

        bool TryGetServerId(EntityUid clientId, out EntityUid serverId);

        EntityUid CreateClientId(EntityUid serverId);

        EntityUid EnsureClientId(EntityUid serverId);

        /// <returns>The list of new entities created.</returns>
        List<EntityUid> ApplyEntityStates(EntityState[]? curEntStates, IEnumerable<EntityUid>? deletions,
            EntityState[]? nextEntStates);
    }
}