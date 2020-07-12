using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components
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

        public readonly Vector2 LinearVelocity;
        public readonly float AngularVelocity;
        public readonly bool Anchored;

        /// <summary>
        ///     Constructs a new state snapshot of a PhysicsComponent.
        /// </summary>
        /// <param name="mass">Current Mass of the entity.</param>
        /// <param name="linearVelocity">Current linear velocity of the entity in meters per second.</param>
        /// <param name="angularVelocity">Current angular velocity of the entity in radians per sec.</param>
        /// <param name="anchored">Whether or not the entity is anchored in place.</param>
        public PhysicsComponentState(float mass, Vector2 linearVelocity, float angularVelocity, bool anchored)
            : base(NetIDs.PHYSICS)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            Mass = (int) Math.Round(mass *1000); // rounds kg to nearest gram
            Anchored = anchored;
        }
    }
}
