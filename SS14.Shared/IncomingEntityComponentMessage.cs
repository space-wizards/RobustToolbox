using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Shared
{
    public struct IncomingEntityComponentMessage
    {
        public uint NetID { get; set; }
        public List<object> MessageParameters { get; set; }

        public IncomingEntityComponentMessage(uint netID, List<object> messageParameters) : this()
        {
            NetID = netID;
            MessageParameters = messageParameters;
        }
    }
}
