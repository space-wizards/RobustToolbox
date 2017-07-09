using System;
using Lidgren.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    /// Arguments for the MessageArrived event. This will be removed in the future.
    /// </summary>
    public class NetMessageArgs : EventArgs
    {
        public NetMessage Message;
        public NetIncomingMessage RawMessage;

        public NetMessageArgs(NetMessage message, NetIncomingMessage rawMessage)
        {
            Message = message;
            RawMessage = rawMessage;
        }
    }
}