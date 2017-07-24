using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class ComponentState : INetSerializableType
    {
        [NonSerialized]
        public float ReceivedTime;

        public uint NetID { get; protected set; }

        public ComponentState(uint netID)
        {
            NetID = netID;
        }

        public ComponentState()
        {
        }
    }
}
