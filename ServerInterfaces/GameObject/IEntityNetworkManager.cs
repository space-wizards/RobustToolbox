using Lidgren.Network;
using SS13_Shared.GO;
using SS13_Shared;
using GO = GameObject;

namespace ServerInterfaces.GOC
{
    public interface IEntityNetworkManager : GO.IEntityNetworkManager
    {
        void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);
    }
}