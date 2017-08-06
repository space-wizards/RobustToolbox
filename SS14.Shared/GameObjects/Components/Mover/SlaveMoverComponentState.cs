using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class SlaveMoverComponentState : ComponentState
    {
        public readonly int? Master;

        public SlaveMoverComponentState(int? master)
            : base(NetIDs.SLAVE_MOVER)
        {
            Master = master;
        }
    }
}
