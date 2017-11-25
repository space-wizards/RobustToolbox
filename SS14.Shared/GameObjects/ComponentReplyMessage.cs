using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    public struct ComponentReplyMessage
    {
        private static ComponentReplyMessage empty = new ComponentReplyMessage(ComponentMessageType.Empty);
        private ComponentMessageType messageType;
        private List<object> paramsList;

        public ComponentReplyMessage(ComponentMessageType messageType, params object[] paramsList) : this()
        {
            ParamsList = paramsList != null ? paramsList.ToList() : new List<object>();
            MessageType = messageType;
        }

        public static ComponentReplyMessage Empty { get => empty; set => empty = value; }
        public ComponentMessageType MessageType { get => messageType; set => messageType = value; }
        public List<object> ParamsList { get => paramsList; set => paramsList = value; }
    }
}
