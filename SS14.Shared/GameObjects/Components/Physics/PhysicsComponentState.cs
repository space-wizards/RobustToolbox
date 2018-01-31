using System;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a PhysicsComponent.
    /// </summary>
    [Serializable]
    public class PhysicsComponentState : ComponentState
    {
        /// <summary>
        ///     Current mass of the entity.
        /// </summary>
        public readonly float Mass;

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        public readonly Vector2 Velocity;

        /// <summary>
        ///     Constructs a new state snapshot of a PhysicsComponent.
        /// </summary>
        /// <param name="mass">Current Mass of the entity.</param>
        /// <param name="velocity">Current Velocity of the entity.</param>
        public PhysicsComponentState(float mass, Vector2 velocity)
            : base(NetIDs.PHYSICS)
        {
            Mass = mass;
            Velocity = velocity;
        }
    }
}
