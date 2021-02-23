using System;

namespace Robust.Shared.Network
{
    /// <summary>
    ///     Defines on which side of the network a net message can be accepted.
    /// </summary>
    [Flags]
    public enum NetMessageAccept : byte
    {
        None = 0,

        /// <summary>
        ///     Message can only be received on the server and it is an error to send it to a client.
        /// </summary>
        Server = 1,

        /// <summary>
        ///     Message can only be received on the client and it is an error to send it to a server.
        /// </summary>
        Client = 2,

        /// <summary>
        ///     Message can be received on both client and server.
        /// </summary>
        Both = Client | Server
    }
}
