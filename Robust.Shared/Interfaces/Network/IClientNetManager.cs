using System;
using Robust.Shared.Network;

namespace Robust.Shared.Interfaces.Network
{
    /// <summary>
    ///     The Client version of the INetManager.
    /// </summary>
    public interface IClientNetManager : INetManager
    {
        /// <summary>
        ///     The NetChannel of the server.
        /// </summary>
        INetChannel? ServerChannel { get; }

        ClientConnectionState ClientConnectState { get; }
        event Action<ClientConnectionState> ClientConnectStateChanged;

        /// <summary>
        ///     The attempted connection by a client to a server failed.
        /// </summary>
        event EventHandler<NetConnectFailArgs> ConnectFailed;

        /// <summary>
        ///     Attempts to connect to the remote server. This does not Restart() the client networking. Make sure
        ///     to Initialize(true) networking before calling this.
        /// </summary>
        /// <param name="host">The IP address of the remote server.</param>
        /// <param name="port">The port the server is listening on.</param>
        /// <param name="userNameRequest">
        ///     The user name to request from the server.
        ///     The server is in no way obliged to actually give this username to the client.
        ///     It's more a "I'd prefer this" kinda deal.
        /// </param>
        void ClientConnect(string host, int port, string userNameRequest);

        /// <summary>
        ///     Disconnects from the server. This does not Restart() the client networking. Make sure
        ///     to Initialize(true) networking before calling this.
        ///     Also cancels in-progress connection attempts.
        /// </summary>
        /// <param name="reason">The reason why disconnect was called.</param>
        void ClientDisconnect(string reason);
    }

    public enum ClientConnectionState : byte
    {
        /// <summary>
        ///     We are not connected and not trying to get connected either. Quite lonely huh.
        /// </summary>
        NotConnecting,

        /// <summary>
        ///     Resolving the DNS query for the address of the server.
        /// </summary>
        ResolvingHost,

        /// <summary>
        ///     Attempting to establish a connection to the server.
        /// </summary>
        EstablishingConnection,

        /// <summary>
        ///     Connection established, going through regular handshake business.
        /// </summary>
        Handshake,

        /// <summary>
        ///     Connection is solid and handshake is done go wild.
        /// </summary>
        Connected
    }
}
