using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class ComponentState
    {
        [NonSerialized]
        private float _receivedTime;

        public float ReceivedTime
        {
            get => _receivedTime;
            set => _receivedTime = value;
        }

        public uint NetID { get; }


        public ComponentState(uint netID)
        {
            NetID = netID;
        }

        public ComponentState()
        {
        }
    }
}
