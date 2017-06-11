using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using GO = SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.GOC
{
    public interface IEntityNetworkManager : GO.IEntityNetworkManager, IIoCInterface
    {
        void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);

        /// <summary>
        /// Sends a message to the relevant system(s) on the target client.
        /// </summary>
        void SendSystemNetworkMessage(EntitySystemMessage message, NetConnection targetConnection = null, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered);
    }
}
