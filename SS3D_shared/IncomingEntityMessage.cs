using Lidgren.Network;
namespace SS13_Shared
{
    public struct ClientIncomingEntityMessage
    {
        public int Uid;
        public EntityMessage MessageType;
        public object Message;

        public ClientIncomingEntityMessage(int uid, EntityMessage messageType, object message)
        {
            Uid = uid;
            MessageType = messageType;
            Message = message;
        }

        public static ClientIncomingEntityMessage Null = new ClientIncomingEntityMessage(0, EntityMessage.Null, null);

    }

    public struct ServerIncomingEntityMessage
    {
        public int uid;
        public EntityMessage messageType;
        public object message;
        public NetConnection client;

        public ServerIncomingEntityMessage(int _uid, EntityMessage _messageType, object _message, NetConnection _client)
        {
            uid = _uid;
            messageType = _messageType;
            message = _message;
            client = _client;
        }

        public static ServerIncomingEntityMessage Null = new ServerIncomingEntityMessage(0, EntityMessage.Null, null, null);

    }
}
