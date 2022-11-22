using System;
using Robust.Shared.Network;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Manages the sending and receiving of network messages between the server and client(s).
    /// </summary>
    public interface IEntityNetworkManager
    {
        /// <summary>
        /// This event is raised when a system message comes in from the network.
        /// </summary>
        event EventHandler<object> ReceivedSystemMessage;

        /// <summary>
        /// Initializes networking for this manager. This should only be called once.
        /// </summary>
        void SetupNetworking();

        /// <summary>
        /// Sends an Entity System Message to relevant System(s).
        /// Client: Sends the message to the relevant server side System(s).
        /// Server: Sends the message to the relevant systems of all connected clients.
        /// Server: Use the alternative overload to send to a single client.
        /// </summary>
        /// <param name="message">Message that should be sent.</param>
        /// <param name="recordReplay">Whether or not this message should be saved to replays.</param>
        void SendSystemNetworkMessage(EntityEventArgs message, bool recordReplay = true);

        void SendSystemNetworkMessage(EntityEventArgs message, uint sequence)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sends an Entity System Message to relevant System on a client.
        /// Server: Sends the message to the relevant systems of the client on <paramref name="channel"/>
        /// </summary>
        /// <param name="message">Message that should be sent.</param>
        /// <param name="channel">The client to send the message to.</param>
        /// <exception cref="NotSupportedException">
        ///    Thrown if called on the client.
        /// </exception>
        void SendSystemNetworkMessage(EntityEventArgs message, INetChannel channel);
    }
}
