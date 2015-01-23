using Lidgren.Network;
using System;

namespace SS14.Shared
{
    public class IncomingNetworkMessageArgs : EventArgs
    {
        public NetIncomingMessage Message;

        public IncomingNetworkMessageArgs(NetIncomingMessage message)
        {
            Message = message;
        }
    }
}