using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Robust.Server.GameObjects
{
    public interface IServerEntityNetworkManager : IEntityNetworkManager
    {
        uint GetLastMessageSequence(ICommonSession session);
    }
}
