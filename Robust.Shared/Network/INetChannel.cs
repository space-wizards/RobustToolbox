using System;
using System.Net;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Network
{
    /// <summary>
    ///     A network channel between this peer and a remote peer.
    /// </summary>
    public interface INetChannel
    {
        /// <summary>
        ///     The NetPeer this belongs to.
        /// </summary>
        INetManager NetPeer { get; }

        /// <summary>
        ///     The Unique Identifier of the connection.
        /// </summary>
        long ConnectionId { get; }

        /// <summary>
        ///     The IP end point.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        ///     The session ID for this channel.
        ///     On the server, this is the session ID for this client.
        ///     On the client, this is the session ID for the client.
        /// </summary>
        [ViewVariables]
        NetUserId UserId { get; }

        [ViewVariables]
        string UserName { get; }

        [ViewVariables]
        LoginType AuthType { get; }

        /// <summary>
        ///     Offset between local RealTime and remote RealTime.
        /// </summary>
        TimeSpan RemoteTimeOffset { get; }

        /// <summary>
        ///     Remote RealTime.
        /// </summary>
        TimeSpan RemoteTime { get; }

        /// <summary>
        ///     Average round trip time in milliseconds between the remote peer and us.
        /// </summary>
        [ViewVariables]
        short Ping { get; }

        /// <summary>
        ///     Whether or not the channel is currently connected to a remote peer.
        /// </summary>
        [ViewVariables]
        bool IsConnected { get; }

        NetUserData UserData { get; }

        /// <summary>
        ///     Has the serializer handshake completed and <see cref="INetManager.Connected"/> been ran?
        /// </summary>
        bool IsHandshakeComplete { get; }

        /// <summary>
        /// Diagnostic indicating the maximum transmission unit being used for this connection.
        /// </summary>
        /// <seealso cref="CVars.NetMtu"/>
        [ViewVariables]
        public int CurrentMtu { get; }

        /// <summary>
        ///     Creates a new NetMessage to be filled up and sent.
        /// </summary>
        /// <typeparam name="T">The derived NetMessage type to send.</typeparam>
        /// <returns>A new instance of the net message.</returns>
        [Obsolete("Just new NetMessage directly")]
        T CreateNetMessage<T>()
            where T : NetMessage, new();

        /// <summary>
        ///     Sends a NetMessage over this NetChannel.
        /// </summary>
        /// <param name="message">The NetMessage to send.</param>
        void SendMessage(NetMessage message);

        /// <summary>
        ///     Disconnects this channel from the remote peer.
        /// </summary>
        /// <param name="reason">Reason why it was disconnected.</param>
        void Disconnect(string reason);

        /// <summary>
        ///     Disconnects this channel from the remote peer.
        /// </summary>
        /// <param name="reason">Reason why it was disconnected.</param>
        /// <param name="sendBye">If false, we ghost the remote client and don't tell them they got disconnected properly.</param>
        void Disconnect(string reason, bool sendBye);
    }
}
