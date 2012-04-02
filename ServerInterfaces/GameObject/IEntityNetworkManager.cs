using Lidgren.Network;
using SS13_Shared.GO;
using SS13_Shared;

namespace ServerInterfaces.GameObject
{
    public interface IEntityNetworkManager
    {
        NetOutgoingMessage CreateEntityMessage();
        void SendToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);
        void SendMessage(NetOutgoingMessage message, NetConnection recipient, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>   
        /// <param name="family">Family of the component sending the message</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="recipient">Client connection to send to. If null, send to all.</param>
        /// <param name="messageParams">Parameters of the message</param>
        void SendComponentNetworkMessage(IEntity sendingEntity, ComponentFamily family, NetDeliveryMethod method,
                                                         NetConnection recipient, params object[] messageParams);

        /// <summary>
        /// Handles an incoming entity message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //IncomingEntityMessage HandleEntityNetworkMessage(NetIncomingMessage message);

        /// <summary>
        /// Handles an incoming entity component message
        /// </summary>
        /// <param name="message"></param>
        //IncomingEntityComponentMessage HandleEntityComponentNetworkMessage(NetIncomingMessage message);
    }
}