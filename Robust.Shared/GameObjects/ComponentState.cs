using Robust.Shared.Serialization;
using System;

namespace Robust.Shared.GameObjects
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
