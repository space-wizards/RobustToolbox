using System;
using Robust.Shared.Network.Messages;

namespace Robust.Shared.GameObjects
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
