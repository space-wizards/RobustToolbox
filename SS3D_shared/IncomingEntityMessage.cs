using System;
using Lidgren.Network;
namespace SS13_Shared
{
    public class IncomingEntityMessage
    {
        public int Uid;
        public EntityMessage MessageType;
        public object Message;
        public DateTime LastProcessingAttempt;
        public DateTime ReceivedTime;
        public ushort Expires;
        public NetConnection Sender;
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
        
        public static IncomingEntityMessage Null = new IncomingEntityMessage(0, EntityMessage.Null, null, null);
    }
}
