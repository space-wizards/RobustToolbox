using Lidgren.Network;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Shared.GameObjects
{
    public interface IEntityNetworkManager
    {
        NetOutgoingMessage CreateEntityMessage();

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>
        /// <param name="family">Family of the component sending the message</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="messageParams">Parameters of the message</param>
        void SendComponentNetworkMessage(IEntity sendingEntity, ComponentFamily family,
                                         NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered,
                                         params object[] messageParams);

        /// <summary>
        /// Sends an Entity System Message to relevant System(s).
        /// Client: Sends the message to the relevant serverside System(s).
        /// Server: Sends the message to the relevant systems of all connected clients.
        /// Server: Use the alternative overload to send to a single client.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message</param>
        /// <param name="targetSystem">Type of the System that should recieve the message. Also includes derived systems.</param>
        /// <param name="message">Message that should be sent.</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="messageParams">Parameters of the message</param>
        void SendSystemNetworkMessage(EntitySystemMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered);

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>
        /// <param name="family">Family of the component sending the message</param>
        /// <param name="method">Net delivery method -- if null, defaults to NetDeliveryMethod.ReliableUnordered</param>
        /// <param name="recipient">Intended recipient of the message</param>
        /// <param name="messageParams">Parameters of the message</param>
        void SendDirectedComponentNetworkMessage(IEntity sendingEntity, ComponentFamily family, NetDeliveryMethod method,
                                                 NetConnection recipient, params object[] messageParams);

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        IncomingEntityMessage HandleEntityNetworkMessage(NetIncomingMessage message);

        /// <summary>
        /// Handles an incoming entity component message
        /// </summary>
        /// <param name="message">Raw network message</param>
        /// <returns>An IncomingEntityComponentMessage object</returns>
        IncomingEntityComponentMessage HandleEntityComponentNetworkMessage(NetIncomingMessage message);

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        void SendEntityNetworkMessage(IEntity sendingEntity, EntityMessage type, params object[] list);

        void SendMessage(NetOutgoingMessage message, NetConnection recipient,
                         NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered);
    }
}
