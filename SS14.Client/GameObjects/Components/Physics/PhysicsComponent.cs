using System;
using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics.
    /// </summary>
    internal class PhysicsComponent : ClientComponent
    {
        private Vector2 _velocity = Vector2.Zero;

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
        public Vector2 Velocity
        {
            get => _velocity;
            set
            {
                // Empty setter because nothing should be modifying this value on the client.
                //TODO: This property needs to be readonly.
            }
        }

        /// <inheritdoc />
        public override void OnAdd(IEntity owner)
        {
            // This component requires that the entity has an AABB.
            if (!owner.HasComponent<HitboxComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PhysicsComponent)} requires {nameof(HitboxComponent)}. ");

            base.OnAdd(owner);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (PhysicsComponentState) state;
            Mass = newState.Mass;
            _velocity = newState.Velocity;
        }
    }
}
