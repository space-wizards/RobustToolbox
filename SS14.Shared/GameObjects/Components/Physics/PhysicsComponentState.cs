using System;

namespace SS14.Shared.GameObjects.Components.Physics
{
    [Serializable]
    public class PhysicsComponentState : ComponentState
    {
        public float Mass;

        public PhysicsComponentState(float mass)
            : base(NetIDs.PHYSICS)
        {
            Mass = mass;
        }
    }
}
