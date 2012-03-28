using SS13_Shared.GO;

namespace ServerInterfaces.MessageLogging
{
    public interface IMessageLogger
    {
        void LogOutgoingComponentNetMessage(long clientUID, int uid, ComponentFamily family, object[] parameters);
        void LogIncomingComponentNetMessage(long clientUID, int uid, SS13_Shared.EntityMessage entityMessage, ComponentFamily componentFamily, object[] parameters);
        void LogComponentMessage(int uid, ComponentFamily senderfamily, string sendertype, ComponentMessageType type);
        void Ping();
    }
}
