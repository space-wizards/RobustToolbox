using Lidgren.Network;
using SS14.Shared.Network.Messages;
using System;

namespace SS14.Shared
{
    public class IncomingEntityMessage
    {
        public MsgEntity Message { get; set; }
        public ushort Expires { get; set; }
        public DateTime LastProcessingAttempt { get; set; }
        public DateTime ReceivedTime { get; set; }
        
        public IncomingEntityMessage(MsgEntity message)
        {
            Message = message;
            LastProcessingAttempt = DateTime.Now;
            ReceivedTime = DateTime.Now;
            Expires = 30;
        }
    }
}
