using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.MessageLogging
{
    public interface IMessageLogger : IIoCInterface
    {
        void LogOutgoingComponentNetMessage(int uid, ComponentFamily family, object[] parameters);

        void LogIncomingComponentNetMessage(int uid, EntityMessage entityMessage, ComponentFamily componentFamily,
                                            object[] parameters);

        void LogComponentMessage(int uid, ComponentFamily senderfamily, string sendertype, ComponentMessageType type);
        void Ping();
    }
}
