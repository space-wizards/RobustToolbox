using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a PhysicsComponent.
    /// </summary>
    [Serializable, NetSerializable]
    public class PhysicsComponentState : ComponentState
    {
        /// <summary>
        ///     Current mass of the entity, stored in grams.
        /// </summary>
        public readonly int Mass;

        /// <summary>
        ///     Current linear velocity of the entity.
        /// </summary>
        public readonly Vector2 LinearVelocity;

        /// <summary>
        ///     Current angular velocity of the entity.
        /// </summary>
        public readonly float AngularVelocity;

        /// <summary>
        ///     Constructs a new state snapshot of a PhysicsComponent.
        /// </summary>
        /// <param name="mass">Current Mass of the entity.</param>
        /// <param name="velocity">Current Velocity of the entity.</param>
        public PhysicsComponentState(float mass, Vector2 linearVelocity, float angularVelocity)
            : base(NetIDs.PHYSICS)
        {
            Mass = (int) Math.Round(mass *1000); // rounds kg to nearest gram
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
        }
    }
}
