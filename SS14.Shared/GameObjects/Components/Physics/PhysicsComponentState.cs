using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class PhysicsComponentState : ComponentState
    {
        public readonly float Mass;

        public PhysicsComponentState(float mass)
            : base(NetIDs.PHYSICS)
        {
            Mass = mass;
        }
    }
}
