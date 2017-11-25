using Lidgren.Network;
using SS14.Shared.Network.Messages;
using System;

namespace SS14.Shared
{
    public class IncomingEntityMessage
    {
        private MsgEntity message;
        private ushort expires;
        private DateTime lastProcessingAttempt;
        private DateTime receivedTime;

        public MsgEntity Message { get => message; set => message = value; }
        public ushort Expires { get => expires; set => expires = value; }
        public DateTime ReceivedTime { get => receivedTime; set => receivedTime = value; }
        public DateTime LastProcessingAttempt { get => lastProcessingAttempt; set => lastProcessingAttempt = value; }

        public IncomingEntityMessage(MsgEntity message)
        {
            Message = message;
            LastProcessingAttempt = DateTime.Now;
            ReceivedTime = DateTime.Now;
            Expires = 30;
        }
    }
}
