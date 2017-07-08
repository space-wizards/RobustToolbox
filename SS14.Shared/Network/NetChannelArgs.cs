using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network
{
    public class NetChannelArgs : EventArgs
    {
        public readonly INetChannel Channel;

        public NetChannelArgs(INetChannel channel)
        {
            Channel = channel;
        }
    }

    public class NetConnectingArgs : EventArgs
    {
        public bool Deny;
        public readonly string Ip;

        public NetConnectingArgs(string ip)
        {
            Ip = ip;
        }
    }
}
