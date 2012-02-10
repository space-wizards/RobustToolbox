using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using SS13_Shared;

namespace MessagingProfiler
{
    public class LogItem
    {
        public long clientID { get; set; }
        public int entityID { get; set; }
        public EntityMessage entityMessageType { get; set; }
        public ComponentFamily componentFamily { get; set; }
        public string senderType { get; set; }
        public ComponentMessageType messageType { get; set; }
        public object[] parameters { get; set; }

        public LogItem()
        {
            clientID = 0;
            entityID = 0;
            entityMessageType = EntityMessage.Null;
            componentFamily = ComponentFamily.Generic;
            senderType = "";
            messageType = ComponentMessageType.Null;
            parameters = null;
        }
    }
}
