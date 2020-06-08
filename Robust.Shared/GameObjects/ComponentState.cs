using Robust.Shared.Serialization;
using System;

namespace Robust.Shared.GameObjects
{

    [Serializable, NetSerializable]
    public abstract class ComponentState
    {

        public abstract uint NetID { get; }

        protected ComponentState()
        {
        }

    }

    [Serializable, NetSerializable]
    internal sealed class NetIdComponentState : ComponentState
    {

        public override uint NetID { get; }

        public NetIdComponentState(uint netId)
        {
            NetID = netId;
        }

    }

}
