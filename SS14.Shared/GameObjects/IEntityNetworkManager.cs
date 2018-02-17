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
        /// <param name="entity"></param>
        /// <param name="component">Component that sent the message.</param>
        /// <param name="message">Message to send.</param>
        void SendComponentNetworkMessage(IEntity entity, IComponent component, ComponentMessage message);

        /// <summary>
        /// Sends an arbitrary entity network message
        /// </summary>
        /// <param name="entity">The entity the message is going from(and to, on the other end)</param>
        /// <param name="message">Message that should be sent.</param>
        void SendEntityNetworkMessage(IEntity entity, EntityEventArgs message);

        /// <summary>
        /// Sends an Entity System Message to relevant System(s).
        /// Client: Sends the message to the relevant server side System(s).
        /// Server: Sends the message to the relevant systems of all connected clients.
        /// Server: Use the alternative overload to send to a single client.
        /// </summary>
        /// <param name="message">Message that should be sent.</param>
        void SendSystemNetworkMessage(EntitySystemMessage message);

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on all clients.
        /// </summary>
        /// <param name="entity">Entity sending the message (also entity to send to)</param>
        /// <param name="component">Component that sent the message.</param>
        /// <param name="channel">Intended recipient of the message. If this is null, broadcast the message to all clients.</param>
        /// <param name="message">Message to send.</param>
        void SendDirectedComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component, ComponentMessage message);

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        IncomingEntityMessage HandleEntityNetworkMessage(MsgEntity message);
    }
}
