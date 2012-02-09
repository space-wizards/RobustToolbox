using System;
using Lidgren.Network;

namespace SS13_Shared
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
