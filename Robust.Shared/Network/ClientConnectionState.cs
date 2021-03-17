namespace Robust.Shared.Network
{
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
