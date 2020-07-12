using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.Interfaces.GameObjects
{
    public interface IServerEntityNetworkManager : IEntityNetworkManager
    {
        uint GetLastMessageSequence(IPlayerSession session);
    }
}
