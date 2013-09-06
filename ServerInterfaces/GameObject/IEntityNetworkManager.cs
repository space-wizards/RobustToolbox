using System;
using Lidgren.Network;
using SS13_Shared.GO;
using GO = GameObject;

namespace ServerInterfaces.GOC
{
    public interface IEntityNetworkManager : GO.IEntityNetworkManager
    {
        void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);

        /// <summary>
        /// Sends a message to the relevant system(s) on the target client.
        /// </summary>
        void SendSystemNetworkMessage(EntitySystemMessage message, NetConnection targetConnection = null, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered);
    }
}