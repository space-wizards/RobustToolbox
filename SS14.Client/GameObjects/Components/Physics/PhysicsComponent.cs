using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics. This behavior overrides
    ///     the BoundingBoxComponent behavior of making the entity static.
    /// </summary>
    internal class PhysicsComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <inheritdoc />
        public override Type StateType => typeof(PhysicsComponentState);

        /// <summary>
        ///     Current mass of the entity.
        /// </summary>
        public float Mass { get; private set; }

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        public Vector2 Velocity { get; private set; }

        /// <inheritdoc />
        public override void OnAdd(IEntity owner)
        {
            // This component requires that the entity has an AABB.
            if (!owner.HasComponent<BoundingBoxComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PhysicsComponent)} requires {nameof(BoundingBoxComponent)}. ");

            base.OnAdd(owner);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (PhysicsComponentState)state;
            Mass = newState.Mass;
            Velocity = newState.Velocity;
        }
    }
}
