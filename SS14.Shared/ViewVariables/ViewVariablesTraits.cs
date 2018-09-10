using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.ViewVariables
{
    [Serializable, NetSerializable]
    public enum ViewVariablesTraits
    {
        Members,
        Enumerable,
        Entity,
    }
}
