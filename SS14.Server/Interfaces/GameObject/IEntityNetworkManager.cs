using Lidgren.Network;
using SS14.Shared.GameObjects;
using GO = SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.GOC
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
