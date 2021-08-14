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
        Server = 1 << 0,

        /// <summary>
        ///     Message can only be received on the client and it is an error to send it to a server.
        /// </summary>
        Client = 1 << 1,

        /// <summary>
        ///     Message can be received on both client and server.
        /// </summary>
        Both = Client | Server,

        /// <summary>
        ///     Message is used during connection handshake and may be sent before the initial handshake is completed.
        /// </summary>
        /// <remarks>
        ///     There is a window of time between the initial authentication handshake and serialization handshake,
        ///     where the connection *does* have an INetChannel.
        ///     During this handshake messages are still blocked however unless this flag is sent on the message type.
        /// </remarks>
        Handshake = 1 << 2,
    }
}
