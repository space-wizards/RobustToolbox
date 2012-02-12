using SS13_Shared.GO;

namespace ClientInterfaces.MessageLogging
{
    public interface IMessageLogger
    {
        void LogOutgoingComponentNetMessage(int uid, ComponentFamily family, object[] parameters);
        void LogIncomingComponentNetMessage(int uid, SS13_Shared.EntityMessage entityMessage, ComponentFamily componentFamily, object[] parameters);
        void LogComponentMessage(int uid, ComponentFamily senderfamily, string sendertype, ComponentMessageType type);
    }
}
