using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public class PhysicsComponentState : ComponentState
    {
        public bool Enabled;
        public bool SleepingAllowed;
        public BodyType BodyType;
        public bool IsBullet;
        public bool IgnoreCCD;
        public bool FixedRotation;
        public Sweep Sweep;
        public List<FixtureState> Fixtures;
        public float AngularVelocity;
        public Vector2 LinearVelocity;
        public float InvI;
        public float InvMass;
        public float Torque;
        public float LinearDamping;
        public float AngularDamping;

        public PhysicsComponentState(
            bool enabled,
            bool sleepingAllowed,
            BodyType bodyType,
            bool isBullet,
            bool ignoreCCD,
            bool fixedRotation,
            Sweep sweep,
            List<FixtureState> fixtures,
            float angularVelocity,
            Vector2 linearVelocity,
            float invI,
            float invMass,
            float torque,
            float linearDamping,
            float angularDamping)
            : base(NetIDs.PHYSICS)
        {
            Enabled = enabled;
            SleepingAllowed = sleepingAllowed;
            BodyType = bodyType;
            IsBullet = isBullet;
            IgnoreCCD = ignoreCCD;
            FixedRotation = fixedRotation;
            Sweep = sweep;
            Fixtures = fixtures;
            AngularVelocity = angularVelocity;
            LinearVelocity = linearVelocity;
            InvI = invI;
            InvMass = invMass;
            Torque = torque;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
        }
    }
}
