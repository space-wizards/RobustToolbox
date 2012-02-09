namespace SS13_Shared
{
    public struct IncomingEntityMessage
    {
        public int Uid;
        public EntityMessage MessageType;
        public object Message;

        public IncomingEntityMessage(int uid, EntityMessage messageType, object message)
        {
            Uid = uid;
            MessageType = messageType;
            Message = message;
        }

        public static IncomingEntityMessage Null = new IncomingEntityMessage(0, EntityMessage.Null, null);

    }
}
