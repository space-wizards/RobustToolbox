using System;
using Lidgren.Network;
using SS14.Shared.Network;

namespace SS14.Shared.Interfaces.Network
{
    /// <summary>
    ///     The Client version of the INetManager.
    /// </summary>
    public interface IClientNetManager : INetManager
    {
        /// <summary>
        ///     The NetChannel of the server.
        /// </summary>
        INetChannel ServerChannel { get; }

        /// <summary>
        ///     Called when a new NetMessage is received.
        /// </summary>
        [Obsolete("You should be registering a callback in RegisterNetMessage.")]
        event EventHandler<NetMessageArgs> MessageArrived;

        /// <summary>
        ///     The attempted connection by a client to a server failed.
        /// </summary>
        event EventHandler<NetConnectFailArgs> ConnectFailed;

        /// <summary>
        ///     Attempts to connect to the remote server. This does not Restart() the client networking. Make sure
        ///     to Initialize(true) networking before calling this.
        /// </summary>
        /// <param name="ipAddress">The IP address of the remote server.</param>
        /// <param name="port">The port the server is listening on.</param>
        void ClientConnect(string ipAddress, int port);

        /// <summary>
        ///     Disconnects from the server. This does not Restart() the client networking. Make sure
        ///     to Initialize(true) networking before calling this.
        /// </summary>
        /// <param name="reason">The reason why disconnect was called.</param>
        void ClientDisconnect(string reason);

        /// <summary>
        ///     Sends a message to the server. Make sure to Initialize(true) and Connect() to a server before calling this.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="deliveryMethod"></param>
        void ClientSendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod);

        /// <summary>
        ///     Sends a message to the server. Make sure to Initialize(true) and Connect() to a server before calling this.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="deliveryMethod"></param>
        void ClientSendMessage(NetMessage message, NetDeliveryMethod deliveryMethod);
    }
}
