using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Physics;
using System;

namespace SS14.Client.GameObjects
{
    internal class PhysicsComponent : Component
    {
        public float Mass { get; set; }

        public override Type StateType
        {
            get { return typeof(PhysicsComponentState); }
        }
        public PhysicsComponent()
        {
            Family = ComponentFamily.Physics;
        }

        public override void HandleComponentState(dynamic state)
        {
            Mass = state.Mass;
        }
    }
}
