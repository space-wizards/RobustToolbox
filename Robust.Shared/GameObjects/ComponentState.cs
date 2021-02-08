using Robust.Shared.Serialization;
using System;
using Robust.Shared.Analyzers;

namespace Robust.Shared.GameObjects
{
    [RequiresSerializable]
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
