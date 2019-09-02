using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;

namespace Robust.Shared.GameObjects
{
    public interface IEntityNetworkManager
    {
        /// <summary>
        /// Initializes networking for this manager. This should only be called once.
        /// </summary>
        void SetupNetworking();

        /// <summary>
        /// Allows a component owned by this entity to send a message to a counterpart component on the
        /// counterpart entities on clients.
        /// </summary>
        /// <param name="channel">
        /// Intended recipient of the message. On Server, if this is null, broadcast the message to all clients.
        /// On clients, this should always be null.
        /// </param>
        /// <param name="entity">Entity sending the message (also entity to send to).</param>
        /// <param name="component">Component that sent the message.</param>
        /// <param name="message">Message to send.</param>
        void SendComponentNetworkMessage(INetChannel channel, IEntity entity, IComponent component,
            ComponentMessage message);

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
        /// Sends an Entity System Message to relevant System on a client.
        /// Server: Sends the message to the relevant systems of the client on <paramref name="channel"/>
        /// </summary>
        /// <param name="message">Message that should be sent.</param>
        /// <param name="channel">The client to send the message to.</param>
        /// <exception cref="NotSupportedException">
        ///    Thrown if called on the client.
        /// </exception>
        void SendSystemNetworkMessage(EntitySystemMessage message, INetChannel channel);
    }
}
