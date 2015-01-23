using System;

namespace SS14.Shared.GO.Component.Physics
{
    [Serializable]
    public class PhysicsComponentState : ComponentState
    {
        public float Mass;

        public PhysicsComponentState(float mass)
            : base(ComponentFamily.Physics)
        {
            Mass = mass;
        }
    }
}
