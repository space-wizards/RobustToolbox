using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class ComponentState : INetSerializableType
    {
        [NonSerialized] public float ReceivedTime;
        public ComponentState(ComponentFamily family)
        {
            Family = family;
        }

        public ComponentState()
        {
        }

        public ComponentFamily Family { get; protected set; }
    }
}
