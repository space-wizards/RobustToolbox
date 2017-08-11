using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    internal class PhysicsComponent : Component
    {
        public override string Name => "Physics";
        public override uint? NetID => NetIDs.PHYSICS;
        public float Mass { get; set; }

        public override Type StateType => typeof(PhysicsComponentState);

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            Mass = ((PhysicsComponentState)state).Mass;
        }
    }
}
