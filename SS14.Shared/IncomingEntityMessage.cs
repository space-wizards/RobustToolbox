using Lidgren.Network;
using System;

namespace SS14.Shared
{
    public class IncomingEntityMessage
    {
        public static IncomingEntityMessage Null = new IncomingEntityMessage(0, EntityMessage.Null, null, null);
        public ushort Expires;
        public DateTime LastProcessingAttempt;
        public object Message;
        public EntityMessage MessageType;
        public DateTime ReceivedTime;
        public NetConnection Sender;
        public int Uid;

        public IncomingEntityMessage(int uid, EntityMessage messageType, object message, NetConnection sender)
        {
            Uid = uid;
            MessageType = messageType;
            Message = message;
            LastProcessingAttempt = DateTime.Now;
            ReceivedTime = DateTime.Now;
            Expires = 30;
            Sender = sender;
        }
    }
}