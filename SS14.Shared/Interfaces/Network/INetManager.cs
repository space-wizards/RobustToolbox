using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Network;

namespace SS14.Shared.Interfaces.Network
{
    /// <summary>
    ///     A network server that listens for connections, relays packets, and manages channels.
    /// </summary>
    public interface INetManager
    {
        /// <summary>
        ///     Is this a server, or a client?
        /// </summary>
        bool IsServer { get; }

        /// <summary>
        ///     Is this a client, or a server?
        /// </summary>
        bool IsClient { get; }

        /// <summary>
        ///     Has networking been started?
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        ///     Is there at least one open NetChannel?
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     The statistics of the raw NetPeer.
        /// </summary>
        NetPeerStatistics Statistics { get; }

        /// <summary>
        ///     All of the current connected NetChannels on this peer.
        /// </summary>
        List<INetChannel> Channels { get; }

        /// <summary>
        ///     The number of connected NetChannels on this peer.
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        ///     The port that the peer is listening on.
        /// </summary>
        int Port { get; }

        /// <summary>
        ///     Initializes the server, and starts listening for connections.
        /// </summary>
        /// <param name="isServer">Is this a server, or a client?</param>
        void Initialize(bool isServer);

        /// <summary>
        ///     Starts the server running and listening on the port.
        /// </summary>
        void Startup();

        /// <summary>
        ///     Shuts down this peer, disconnecting all channels.
        /// </summary>
        /// <param name="reason">String describing why the peer was shut down.</param>
        void Shutdown(string reason);

        /// <summary>
        ///     Restarts this peer, disconnecting all channels.
        /// </summary>
        /// <param name="reason">String describing why the peer was restarted.</param>
        void Restart(string reason);

        /// <summary>
        ///     Process all queued packets. Should be called often.
        /// </summary>
        void ProcessPackets();

        /// <summary>
        ///     Broadcasts a message to every connected channel.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        void ServerSendToAll(NetMessage message);

        /// <summary>
        ///     Sends a message to a single channel.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="recipient">Channel to send the message over.</param>
        void ServerSendMessage(NetMessage message, INetChannel recipient);

        /// <summary>
        ///     Sends a message to a collection of channels.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="recipients">Channels to send the message over.</param>
        void ServerSendToMany(NetMessage message, List<INetChannel> recipients);

        /// <summary>
        ///     An incoming connection is being received.
        /// </summary>
        event EventHandler<NetConnectingArgs> Connecting;

        /// <summary>
        ///     A client has just connected to the server.
        /// </summary>
        event EventHandler<NetChannelArgs> Connected;

        /// <summary>
        ///     A client has just disconnected from the server.
        /// </summary>
        event EventHandler<NetChannelArgs> Disconnect;

        #region StringTable

        /// <summary>
        ///     Registers a NetMessage to be sent or received.
        /// </summary>
        /// <typeparam name="T">Type to register.</typeparam>
        /// <param name="name">String ID of the message.</param>
        /// <param name="id">Legacy ID of this message. Will be removed.</param>
        /// <param name="rxCallback">Callback function to process the received message.</param>
        void RegisterNetMessage<T>(string name, int id, ProcessMessage rxCallback = null)
            where T : NetMessage;

        /// <summary>
        ///     Creates a new NetMessage to be sent.
        /// </summary>
        /// <typeparam name="T">Type of NetMessage to send.</typeparam>
        /// <returns>Instance of the NetMessage.</returns>
        T CreateNetMessage<T>() where T : NetMessage;

        #endregion

        #region Obsolete

        /// <summary>
        ///     The raw NetPeer. This is only available for legacy code, you should be using NetChannels.
        /// </summary>
        [Obsolete("You should be using the INetManager interface.")]
        NetPeer Peer { get; }

        /// <summary>
        ///     Gets a new NetOutgoingMessage from the NetPeer.
        /// </summary>
        /// <returns>A new Outgoing message.</returns>
        [Obsolete("You should be using NetMessages.")]
        NetOutgoingMessage CreateMessage();

        /// <summary>
        /// Legacy function for sending a raw packet to a NetConnection. This will be removed.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="client"></param>
        /// <param name="method"></param>
        [Obsolete("You should be using NetMessages.")]
        void ServerSendMessage(NetOutgoingMessage message, NetConnection client, NetDeliveryMethod method);

        /// <summary>
        /// Legacy function for sending a raw packet to all connected NetConnectionss on the peer. This will be removed.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="method"></param>
        [Obsolete("You should be using NetMessages.")]
        void ServerSendToAll(NetOutgoingMessage message, NetDeliveryMethod method);

        #endregion
    }
}
