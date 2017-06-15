using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerEntityNetworkManager : IEntityNetworkManager, IIoCInterface
    {
        void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);

        /// <summary>
        /// Sends a message to the relevant system(s) on the target client.
        /// </summary>
        void SendSystemNetworkMessage(EntitySystemMessage message, NetConnection targetConnection = null, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered);
    }
}
