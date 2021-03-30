using System;
using System.Net;

namespace Robust.Shared.Network
{
    /// <summary>
    /// Arguments for NetChannel events.
    /// </summary>
    public class NetChannelArgs : EventArgs
    {
        /// <summary>
        ///     The channel causing the event.
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
        public bool IsDenied => DenyReason != null;

        public string? DenyReason { get; private set; }

        public NetUserData UserData { get; }

        /// <summary>
        /// The IP of the incoming connection.
        /// </summary>
        public NetUserId UserId => UserData.UserId;
        public string UserName => UserData.UserName;

        public IPEndPoint IP { get; }
        public LoginType AuthType { get; }

        public void Deny(string reason)
        {
            DenyReason = reason;
        }

        /// <summary>
        ///     Constructs a new instance.
        /// </summary>
        /// <param name="data">The user data of the incoming connection.</param>
        /// <param name="ip"></param>
        /// <param name="authType">The type of authentication to use when connecting.</param>
        public NetConnectingArgs(NetUserData data, IPEndPoint ip, LoginType authType)
        {
            UserData = data;
            IP = ip;
            AuthType = authType;
        }
    }

    /// <summary>
    /// Arguments for a failed connection attempt.
    /// </summary>
    public class NetConnectFailArgs : EventArgs
    {
        public NetConnectFailArgs(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }

    public class NetDisconnectedArgs : NetChannelArgs
    {
        public NetDisconnectedArgs(INetChannel channel, string reason) : base(channel)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
