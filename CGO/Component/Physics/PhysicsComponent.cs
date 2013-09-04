using System;
using System.Collections.Generic;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Physics;

namespace CGO
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