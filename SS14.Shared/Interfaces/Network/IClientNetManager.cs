using System;
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
        void ClientSendMessage(NetMessage message);
    }
}
