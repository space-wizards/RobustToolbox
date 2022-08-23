using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects
{
    public interface IServerEntityNetworkManager : IEntityNetworkManager
    {
        uint GetLastMessageSequence(IPlayerSession session);
        List<ushort> GetDeletedComponents(EntityUid uid, GameTick fromTick);
        void CullDeletionHistory(GameTick oldestAck);
    }
}
