using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class PhysicsComponentState : ComponentState
    {
        public readonly bool CanCollide;
        public readonly BodyStatus Status;
        public readonly List<IPhysShape> PhysShapes;
        public readonly bool Hard;


        /// <summary>
        ///     Current mass of the entity, stored in grams.
        /// </summary>
        public readonly int Mass;
        public readonly Vector2 LinearVelocity;
        public readonly float AngularVelocity;
        public readonly bool Anchored;

        /// <summary>
        ///
        /// </summary>
        /// <param name="canCollide"></param>
        /// <param name="status"></param>
        /// <param name="physShapes"></param>
        /// <param name="hard"></param>
        /// <param name="mass">Current Mass of the entity.</param>
        /// <param name="linearVelocity">Current linear velocity of the entity in meters per second.</param>
        /// <param name="angularVelocity">Current angular velocity of the entity in radians per sec.</param>
        /// <param name="anchored">Whether or not the entity is anchored in place.</param>
        public PhysicsComponentState(bool canCollide, BodyStatus status, List<IPhysShape> physShapes, bool hard, float mass, Vector2 linearVelocity, float angularVelocity, bool anchored)
            : base(NetIDs.PHYSICS)
        {
            CanCollide = canCollide;
            Status = status;
            PhysShapes = physShapes;
            Hard = hard;

            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            Mass = (int)Math.Round(mass * 1000); // rounds kg to nearest gram
            Anchored = anchored;
        }
    }
}
