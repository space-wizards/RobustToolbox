using System;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    /// Arguments for NetChannel events.
    /// </summary>
    public class NetChannelArgs : EventArgs
    {
        /// <summary>
        /// The channel causing the event.
        /// </summary>
        public readonly INetChannel Channel;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="channel">The channel causing the event.</param>
        public NetChannelArgs(INetChannel channel)
        {
            Channel = channel;
        }
    }

    /// <summary>
    /// Arguments for incoming connection event.
    /// </summary>
    public class NetConnectingArgs : EventArgs
    {
        /// <summary>
        /// If this is set to true, deny the incoming connection.
        /// </summary>

        /// <summary>
        /// The IP of the incoming connection.
        /// </summary>
        public readonly string Ip;

        public bool Deny { get; set; }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="ip">The IP of the incoming connection.</param>
        public NetConnectingArgs(string ip)
        {
            Ip = ip;
        }
    }

    /// <summary>
    /// Arguments for a failed connection attempt.
    /// </summary>
    public class NetConnectFailArgs : EventArgs
    {
        
    }
}
