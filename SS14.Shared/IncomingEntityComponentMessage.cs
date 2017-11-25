using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Shared
{
    public struct IncomingEntityComponentMessage
    {
        private uint netID;
        private List<object> messageParameters;

        public uint NetID { get => netID; set => netID = value; }
        public List<object> MessageParameters { get => messageParameters; set => messageParameters = value; }

        public IncomingEntityComponentMessage(uint netID, List<object> messageParameters) : this()
        {
            NetID = netID;
            MessageParameters = messageParameters;
        }
    }
}
