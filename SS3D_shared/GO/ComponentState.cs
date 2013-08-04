using System;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO
{
    [Serializable]
    public class ComponentState : INetSerializableType
    {
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