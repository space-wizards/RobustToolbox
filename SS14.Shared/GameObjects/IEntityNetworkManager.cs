using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.GameObjects
{
    public interface IEntityNetworkManager
    {
        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>
        /// <param name="netId"></param>
        /// <param name="messageParams">Parameters of the message</param>
        void SendComponentNetworkMessage(IEntity sendingEntity, uint netId, params object[] messageParams);

        /// <summary>
        /// Sends an Entity System Message to relevant System(s).
        /// Client: Sends the message to the relevant serverside System(s).
        /// Server: Sends the message to the relevant systems of all connected clients.
        /// Server: Use the alternative overload to send to a single client.
        /// </summary>
        /// <param name="message">Message that should be sent.</param>
        void SendSystemNetworkMessage(EntitySystemMessage message);

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="sendingEntity">Entity sending the message (also entity to send to)</param>
        /// <param name="netId"></param>
        /// <param name="recipient">Intended recipient of the message</param>
        /// <param name="messageParams">Parameters of the message</param>
        void SendDirectedComponentNetworkMessage(IEntity sendingEntity, uint netId, INetChannel recipient, params object[] messageParams);

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        IncomingEntityMessage HandleEntityNetworkMessage(MsgEntity message);

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="sendingEntity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="type">Message type</param>
        /// <param name="list">List of parameter objects</param>
        void SendEntityNetworkMessage(IEntity sendingEntity, EntityMessageType type, params object[] list);
    }
}
