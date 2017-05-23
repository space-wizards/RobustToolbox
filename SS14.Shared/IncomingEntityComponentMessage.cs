using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Shared
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
