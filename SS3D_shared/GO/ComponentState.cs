using System;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO
{
    [Serializable]
    public class ComponentState : INetSerializableType
    {
        public ComponentFamily Family { get; protected set; }

        public ComponentState(ComponentFamily family)
        {
            Family = family;
        }

        public ComponentState() {}
    }
}
