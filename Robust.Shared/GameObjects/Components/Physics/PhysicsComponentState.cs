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

        public readonly Vector2 LinearVelocity;
        public readonly float AngularVelocity;

        /// <summary>
        ///     Constructs a new state snapshot of a PhysicsComponent.
        /// </summary>
        /// <param name="mass">Current Mass of the entity.</param>
        public PhysicsComponentState(float mass, Vector2 linearVelocity, float angularVelocity)
            : base(NetIDs.PHYSICS)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            Mass = (int) Math.Round(mass *1000); // rounds kg to nearest gram
        }
    }
}
