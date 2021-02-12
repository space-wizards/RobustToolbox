using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Robust.Shared.Network
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
        ///     Network traffic statistics for the local NetChannel.
        /// </summary>
        NetworkStats Statistics { get; }

        /// <summary>
        ///     All of the current connected NetChannels on this peer.
        /// </summary>
        IEnumerable<INetChannel> Channels { get; }

        /// <summary>
        ///     The number of connected NetChannels on this peer.
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        ///     The port that the peer is listening on.
        /// </summary>
        int Port { get; }

        IReadOnlyDictionary<Type, long> MessageBandwidthUsage { get; }

        void ResetBandwidthMetrics();

        /// <summary>
        ///     Initializes the server, and starts listening for connections.
        /// </summary>
        /// <param name="isServer">Is this a server, or a client?</param>
        void Initialize(bool isServer);

        /// <summary>
        ///     Starts the server running and listening on the port.
        /// </summary>
        void StartServer();

        /// <summary>
        ///     Shuts down this peer, disconnecting all channels.
        /// </summary>
        /// <param name="reason">String describing why the peer was shut down.</param>
        void Shutdown(string reason);

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
        ///     Sends a message to the server. Make sure to Initialize(true) and Connect() to a server before calling this.
        /// </summary>
        /// <param name="message">Message to send.</param>
        void ClientSendMessage(NetMessage message);

        /// <summary>
        ///     An incoming connection is being received.
        /// </summary>
        event Func<NetConnectingArgs, Task> Connecting;

        /// <summary>
        ///     A client has just connected to the server.
        /// </summary>
        event EventHandler<NetChannelArgs> Connected;

        /// <summary>
        ///     A client has just disconnected from the server.
        /// </summary>
        event EventHandler<NetDisconnectedArgs> Disconnect;

        #region StringTable

        /// <summary>
        ///     Registers a NetMessage to be sent or received.
        /// </summary>
        /// <typeparam name="T">Type to register.</typeparam>
        /// <param name="name">String ID of the message.</param>
        /// <param name="rxCallback">Callback function to process the received message.</param>
        /// <param name="accept">
        /// The side of the network this message is accepted on.
        /// If we are not on the side specified, the receive callback will not be registered even if provided.
        /// </param>
        void RegisterNetMessage<T>(string name, ProcessMessage<T>? rxCallback = null,
            NetMessageAccept accept = NetMessageAccept.Both)
            where T : NetMessage;

        /// <summary>
        ///     Creates a new NetMessage to be sent.
        /// </summary>
        /// <remarks>
        ///     This function is thread safe.
        /// </remarks>
        /// <typeparam name="T">Type of NetMessage to send.</typeparam>
        /// <returns>Instance of the NetMessage.</returns>
        T CreateNetMessage<T>() where T : NetMessage;

        #endregion StringTable

    }
}
