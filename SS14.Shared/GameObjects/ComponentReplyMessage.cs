using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    public struct ComponentReplyMessage
    {
        private static ComponentReplyMessage empty = new ComponentReplyMessage(ComponentMessageType.Empty);

        public ComponentReplyMessage(ComponentMessageType messageType, params object[] paramsList) : this()
        {
            ParamsList = paramsList != null ? paramsList.ToList() : new List<object>();
            MessageType = messageType;
        }

        public static ComponentReplyMessage Empty { get; set; }
        public ComponentMessageType MessageType { get; set; }
        public List<object> ParamsList { get; set; }
    }
}
