using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Shared
{
    public struct IncomingEntityComponentMessage
    {
        public uint NetID;
        public List<object> MessageParameters;

        public IncomingEntityComponentMessage(uint netID, List<object> messageParameters)
        {
            NetID = netID;
            MessageParameters = messageParameters;
        }
    }
}
