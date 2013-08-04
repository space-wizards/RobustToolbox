using System.Collections.Generic;
using SS13_Shared.GO;

namespace SS13_Shared
{
    public struct IncomingEntityComponentMessage
    {
        public ComponentFamily ComponentFamily;
        public List<object> MessageParameters;

        public IncomingEntityComponentMessage(ComponentFamily componentFamily, List<object> messageParameters)
        {
            ComponentFamily = componentFamily;
            MessageParameters = messageParameters;
        }
    }
}