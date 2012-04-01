using System.Collections.Generic;
using System.Linq;

namespace SS13_Shared.GO
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

        public static ComponentReplyMessage Empty = new ComponentReplyMessage(ComponentMessageType.Empty);
    }
}
