using System;
using Lidgren.Network;

namespace SS14.Shared.Network
{
    public class NetMessageArgs : EventArgs
    {
        public NetMessage Message;
        public NetIncomingMessage RawMessage;

        public NetMessageArgs(NetMessage message, NetIncomingMessage rawMessage)
        {
            RawMessage = rawMessage;
        }
    }
}