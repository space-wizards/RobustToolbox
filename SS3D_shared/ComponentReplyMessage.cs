using System.Collections.Generic;
using System.Linq;
using SS13_Shared.GO;

namespace SS13_Shared
{
    public struct ComponentReplyMessage
    {
        public ComponentMessageType MessageType;
        public List<object> ParamsList;

        public ComponentReplyMessage(ComponentMessageType messageType, params object[] paramsList)
        {
            ParamsList = paramsList != null ? paramsList.ToList() : new List<object>();
            MessageType = messageType;
        }

        public static ComponentReplyMessage Null = new ComponentReplyMessage(ComponentMessageType.Empty);
    }
}
