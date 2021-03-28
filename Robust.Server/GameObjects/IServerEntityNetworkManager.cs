using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    public interface IServerEntityNetworkManager : IEntityNetworkManager
    {
        uint GetLastMessageSequence(IPlayerSession session);
    }
}
