using SS14.Shared;
using SS14.Shared.GO;

namespace SS14.Tools.MessagingProfiler
{
    public class LogItem
    {
        public int LogItemId { get; set; }
        public long ClientId { get; set; }
        public int EntityId { get; set; }
        public EntityMessage EntityMessageType { get; set; }
        public ComponentFamily ComponentFamily { get; set; }
        public string SenderType { get; set; }
        public ComponentMessageType MessageType { get; set; }
        public object[] Parameters { get; set; }
        public enum LogMessageType
        {
            None,
            ClientComponentMessage,
            ClientRecievedNetMessage,
            ClientSentNetMessage,
            ServerComponentMessage,
            ServerRecievedNetMessage,
            ServerSentNetMessage
        }
        public LogMessageType MessageSource { get; set; }

        public LogItem()
        {
            LogItemId = LogHolder.Singleton.NextId;
            ClientId = 0;
            EntityId = 0;
            EntityMessageType = EntityMessage.Null;
            ComponentFamily = ComponentFamily.Generic;
            SenderType = "";
            MessageType = ComponentMessageType.Null;
            Parameters = null;
            MessageSource = LogMessageType.None;
        }
    }
}
