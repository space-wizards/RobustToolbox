using Lidgren.Network;
using GO = GameObject;

namespace ServerInterfaces.GOC
{
    public interface IEntityNetworkManager : GO.IEntityNetworkManager
    {
        void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);
    }
}