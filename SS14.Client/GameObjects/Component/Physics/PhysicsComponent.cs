using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Physics;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    internal class PhysicsComponent : ClientComponent
    {
        public override string Name => "Physics";
        public override uint? NetID => NetIDs.PHYSICS;
        public float Mass { get; set; }

        public override Type StateType => typeof(PhysicsComponentState);

        public override void HandleComponentState(dynamic state)
        {
            Mass = state.Mass;
        }
    }
}
