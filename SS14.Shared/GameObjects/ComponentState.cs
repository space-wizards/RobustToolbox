using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class ComponentState
    {
        [NonSerialized]
        private float receivedTime;

        public uint NetID { get; protected set; }
        public global::System.Single ReceivedTime { get => receivedTime; set => receivedTime = value; }

        public ComponentState(uint netID)
        {
            NetID = netID;
        }

        public ComponentState()
        {
        }
    }
}
