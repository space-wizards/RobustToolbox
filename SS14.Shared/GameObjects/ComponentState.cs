using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class ComponentState
    {
        public uint NetID { get; }

        public ComponentState(uint netID)
        {
            NetID = netID;
        }
    }
}
