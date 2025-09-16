using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects
{
    public interface IServerEntityNetworkManager : IEntityNetworkManager
    {
        uint GetLastMessageSequence(ICommonSession session);
        GameTick GetLastAppliedTick(ICommonSession session);
    }
}
