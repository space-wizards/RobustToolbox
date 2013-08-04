using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;

namespace ServerInterfaces.Network
{
    public interface ISS13NetServer
    {
        /// <summary>
        /// Gets the NetPeerStatus of the NetPeer
        /// </summary>
        NetPeerStatus Status { get; }

        /// <summary>
        /// Signalling event which can be waited on to determine when a message is queued for reading.
        /// Note that there is no guarantee that after the event is signaled the blocked thread will 
        /// find the message in the queue. Other user created threads could be preempted and dequeue 
        /// the message before the waiting thread wakes up.
        /// </summary>
        AutoResetEvent MessageReceivedEvent { get; }

        /// <summary>
        /// Gets a unique identifier for this NetPeer based on Mac address and ip/port. Note! Not available until Start() has been called!
        /// </summary>
        long UniqueIdentifier { get; }

        /// <summary>
        /// Gets the port number this NetPeer is listening and sending on, if Start() has been called
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Returns an UPnP object if enabled in the NetPeerConfiguration
        /// </summary>
        NetUPnP UPnP { get; }

        /// <summary>
        /// Gets or sets the application defined object containing data about the peer
        /// </summary>
        object Tag { get; set; }

        /// <summary>
        /// Gets a copy of the list of connections
        /// </summary>
        List<NetConnection> Connections { get; }

        /// <summary>
        /// Gets the number of active connections
        /// </summary>
        int ConnectionsCount { get; }

        /// <summary>
        /// Statistics on this NetPeer since it was initialized
        /// </summary>
        NetPeerStatistics Statistics { get; }

        /// <summary>
        /// Gets the configuration used to instanciate this NetPeer
        /// </summary>
        NetPeerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the socket, if Start() has been called
        /// </summary>
        Socket Socket { get; }

        void SendToAll(NetOutgoingMessage message);
        void SendMessage(NetOutgoingMessage message, NetConnection client);
        void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients);

        /// <summary>
        /// Send a message to all connections
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">How to deliver the message</param>
        void SendToAll(NetOutgoingMessage msg, NetDeliveryMethod method);

        /// <summary>
        /// Send a message to all connections except one
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="except">Don't send to this particular connection</param>
        /// <param name="sequenceChannel">Which sequence channel to use for the message</param>
        void SendToAll(NetOutgoingMessage msg, NetConnection except, NetDeliveryMethod method, int sequenceChannel);

        /// <summary>
        /// Returns a string that represents this object
        /// </summary>
        string ToString();

        /// <summary>
        /// Send a message to a specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        NetSendResult SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method);

        /// <summary>
        /// Send a message to a specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        NetSendResult SendMessage(NetOutgoingMessage msg, NetConnection recipient, NetDeliveryMethod method,
                                  int sequenceChannel);

        /// <summary>
        /// Send a message to a list of connections
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipients">The list of recipients to send to</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        void SendMessage(NetOutgoingMessage msg, List<NetConnection> recipients, NetDeliveryMethod method,
                         int sequenceChannel);

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        void SendUnconnectedMessage(NetOutgoingMessage msg, string host, int port);

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        void SendUnconnectedMessage(NetOutgoingMessage msg, IPEndPoint recipient);

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        void SendUnconnectedMessage(NetOutgoingMessage msg, IList<IPEndPoint> recipients);

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        NetOutgoingMessage CreateMessage();

        /// <summary>
        /// Creates a new message for sending and writes the provided string to it
        /// </summary>
        NetOutgoingMessage CreateMessage(string content);

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        /// <param name="initialCapacity">initial capacity in bytes</param>
        NetOutgoingMessage CreateMessage(int initialCapacity);

        /// <summary>
        /// Recycles a NetIncomingMessage instance for reuse; taking pressure off the garbage collector
        /// </summary>
        void Recycle(NetIncomingMessage msg);

        /// <summary>
        /// Emit a discovery signal to all hosts on your subnet
        /// </summary>
        void DiscoverLocalPeers(int serverPort);

        /// <summary>
        /// Emit a discovery signal to a single known host
        /// </summary>
        bool DiscoverKnownPeer(string host, int serverPort);

        /// <summary>
        /// Emit a discovery signal to a single known host
        /// </summary>
        void DiscoverKnownPeer(IPEndPoint endpoint);

        /// <summary>
        /// Send a discovery response message
        /// </summary>
        void SendDiscoveryResponse(NetOutgoingMessage msg, IPEndPoint recipient);

        /// <summary>
        /// Send NetIntroduction to hostExternal and clientExternal; introducing client to host
        /// </summary>
        void Introduce(
            IPEndPoint hostInternal,
            IPEndPoint hostExternal,
            IPEndPoint clientInternal,
            IPEndPoint clientExternal,
            string token);

        /// <summary>
        /// Binds to socket and spawns the networking thread
        /// </summary>
        void Start();

        /// <summary>
        /// Get the connection, if any, for a certain remote endpoint
        /// </summary>
        NetConnection GetConnection(IPEndPoint ep);

        /// <summary>
        /// Read a pending message from any connection, blocking up to maxMillis if needed
        /// </summary>
        NetIncomingMessage WaitMessage(int maxMillis);

        /// <summary>
        /// Read a pending message from any connection, if any
        /// </summary>
        NetIncomingMessage ReadMessage();

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        NetConnection Connect(string host, int port);

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        NetConnection Connect(string host, int port, NetOutgoingMessage hailMessage);

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        NetConnection Connect(IPEndPoint remoteEndpoint);

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        NetConnection Connect(IPEndPoint remoteEndpoint, NetOutgoingMessage hailMessage);

        /// <summary>
        /// Disconnects all active connections and closes the socket
        /// </summary>
        void Shutdown(string bye);

        /// <summary>
        /// Call this to register a callback for when a new message arrives
        /// </summary>
        void RegisterReceivedCallback(SendOrPostCallback callback);
    }
}