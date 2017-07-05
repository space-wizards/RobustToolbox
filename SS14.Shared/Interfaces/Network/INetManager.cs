using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Network;
using NetServer = Lidgren.Network.NetServer;

namespace SS14.Shared.Interfaces.Network
{
    /// <summary>
    /// A network server that listens for connections, relays packets, and manages channels.
    /// </summary>
    public interface INetManager
    {
        /// <summary>
        /// Initializes the server, and starts listening for connections.
        /// </summary>
        /// <param name="isServer">Is this a server, or a client?</param>
        void Initialize(bool isServer);

        /// <summary>
        /// Is this a server or a client?
        /// </summary>
        bool IsServer { get; }

        /// <summary>
        /// Process all queued packets. Should be called often.
        /// </summary>
        void ProcessPackets();

        /// <summary>
        /// The first NetChannel on the client, which would be the server.
        /// </summary>
        /// <returns></returns>
        INetChannel GetServerChannel();

        /// <summary>
        /// The statistics of the raw NetPeer.
        /// </summary>
        NetPeerStatistics Statistics { get; }

        /// <summary>
        /// All of the current connected NetChannels on this peer.
        /// </summary>
        List<INetChannel> Channels { get; }

        /// <summary>
        /// The number of connected NetChannels on this peer.
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        /// Broadcasts a message to every connected channel.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        void SendToAll(NetMessage message);

        /// <summary>
        /// Sends a message to a single channel.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="recipient">Channel to send the message over.</param>
        void SendMessage(NetMessage message, INetChannel recipient);

        /// <summary>
        /// Sends a message to a collection of channels.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="recipients">Channels to send the message over.</param>
        void SendToMany(NetMessage message, List<INetChannel> recipients);

        #region StringTable

        /// <summary>
        /// Registers a NetMessage to be sent or received.
        /// </summary>
        /// <typeparam name="T">Type to register.</typeparam>
        /// <param name="name">String ID of the message.</param>
        /// <param name="rxCallback">Callback function to process the received message.</param>
        void RegisterNetMessage<T>(string name, int id, ProcessMessage rxCallback = null)
            where T : NetMessage;

        /// <summary>
        /// Creates a new NetMessage to be sent.
        /// </summary>
        /// <typeparam name="T">Type of NetMessage to send.</typeparam>
        /// <returns>Instance of the NetMessage.</returns>
        T CreateNetMessage<T>() where T : NetMessage;

        #endregion

            /// <summary>
            /// An incoming connection is being received.
            /// </summary>
        event OnConnectingEvent OnConnecting;

        /// <summary>
        /// A client has just connected to the server.
        /// </summary>
        event OnConnectedEvent OnConnected;

        /// <summary>
        /// A client has just disconnected from the server.
        /// </summary>
        event OnDisconnectEvent OnDisconnect;

        #region Obsolete

        /// <summary>
        /// This is only for compatibility with code that has not been migrated to the new system.
        /// </summary>
        [Obsolete("You should be using the INetManager interface.")]
        NetServer Server { get; }
        
        #endregion
    }
}
