using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class PhysicsComponentState : ComponentState
    {
        public readonly bool CanCollide;
        public readonly bool SleepingAllowed;
        public readonly bool FixedRotation;
        public readonly BodyStatus Status;
        public readonly List<Fixture> Fixtures;
        public readonly List<Joint> Joints;

        public readonly Vector2 LinearVelocity;
        public readonly float AngularVelocity;
        public readonly BodyType BodyType;

        /// <summary>
        ///
        /// </summary>
        /// <param name="canCollide"></param>
        /// <param name="sleepingAllowed"></param>
        /// <param name="fixedRotation"></param>
        /// <param name="status"></param>
        /// <param name="fixtures"></param>
        /// <param name="joints"></param>
        /// <param name="linearVelocity">Current linear velocity of the entity in meters per second.</param>
        /// <param name="angularVelocity">Current angular velocity of the entity in radians per sec.</param>
        /// <param name="bodyType"></param>
        public PhysicsComponentState(
            bool canCollide,
            bool sleepingAllowed,
            bool fixedRotation,
            BodyStatus status,
            List<Fixture> fixtures,
            List<Joint> joints,
            Vector2 linearVelocity,
            float angularVelocity,
            BodyType bodyType)
        {
            CanCollide = canCollide;
            SleepingAllowed = sleepingAllowed;
            FixedRotation = fixedRotation;
            Status = status;
            Fixtures = fixtures;
            Joints = joints;

            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            BodyType = bodyType;
        }
    }
}
