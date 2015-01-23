using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GO
{
    public struct ComponentReplyMessage
    {
        public static ComponentReplyMessage Empty = new ComponentReplyMessage(ComponentMessageType.Empty);
        public ComponentMessageType MessageType;
        public List<object> ParamsList;

        public ComponentReplyMessage(ComponentMessageType messageType, params object[] paramsList)
        {
            ParamsList = paramsList != null ? paramsList.ToList() : new List<object>();
            MessageType = messageType;
        }
    }
}