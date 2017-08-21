using Lidgren.Network;
using SS14.Shared.Network.Messages;
using System;

namespace SS14.Shared
{
    public class IncomingEntityMessage
    {
        public MsgEntity Message;
        public ushort Expires;
        public DateTime LastProcessingAttempt;
        public DateTime ReceivedTime;

        public IncomingEntityMessage(MsgEntity message)
        {
            Message = message;
            LastProcessingAttempt = DateTime.Now;
            ReceivedTime = DateTime.Now;
            Expires = 30;
        }
    }
}