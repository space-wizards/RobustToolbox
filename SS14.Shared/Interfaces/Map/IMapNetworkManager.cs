using Lidgren.Network;
using SS14.Shared.IoC;

namespace SS14.Shared.Interfaces.Map
{
    public interface IMapNetworkManager : IIoCInterface
    {
        void HandleNetworkMessage(IMapManager mapManager, NetIncomingMessage message);
        void SendMap(IMapManager mapManager, NetConnection connection);
    }
}
