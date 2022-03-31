/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    // Point-to-point constraint
    // Cdot = v2 - v1
    //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
    // J = [-I -r1_skew I r2_skew ]
    // Identity used:
    // w k % (rx i + ry j) = w * (-ry i + rx j)

    // Angle constraint
    // Cdot = w2 - w1
    // J = [0 0 -1 0 0 1]
    // K = invI1 + invI2

    [Serializable, NetSerializable]
    public sealed class FrictionJointState : JointState
    {
        public float MaxForce { get; }
        public float MaxTorque { get; }

        public override Joint GetJoint()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var bodyA = entityManager.GetComponent<PhysicsComponent>(UidA);
            var bodyB = entityManager.GetComponent<PhysicsComponent>(UidB);

            var joint = new FrictionJoint(bodyA, bodyB)
            {
                MaxForce = MaxForce,
                MaxTorque = MaxTorque,
                LocalAnchorA = LocalAnchorA,
                LocalAnchorB = LocalAnchorB,
            };
            return joint;
        }
    }

    /// <summary>
    /// Friction joint. This is used for top-down friction.
    /// It provides 2D translational friction and angular friction.
    /// </summary>
    public sealed class FrictionJoint : Joint, IEquatable<FrictionJoint>
    {
        // Solver shared
        private Vector2 _linearImpulse;

        private float _angularImpulse;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _rA;
        private Vector2 _rB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private float _angularMass;
        private Vector2[] _linearMass = new Vector2[2];

        public override JointType JointType => JointType.Friction;

        /// <summary>
        ///     The maximum friction force in N.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxForce")]
        public float MaxForce { get; set; }

        /// <summary>
        ///     The maximum friction torque in N-m.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTorque")]
        public float MaxTorque { get; set; }

        public FrictionJoint() {}

        /// <summary>
        /// Constructor for FrictionJoint.
        /// </summary>
        /// <param name="bodyA"></param>
        /// <param name="bodyB"></param>
        /// <param name="anchor"></param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public FrictionJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchor, bool useWorldCoordinates = false)
            : base(bodyA.Owner, bodyB.Owner)
        {
            if (useWorldCoordinates)
            {
                LocalAnchorA = BodyA.GetLocalPoint(anchor);
                LocalAnchorB = BodyB.GetLocalPoint(anchor);
            }
            else
            {
                LocalAnchorA = anchor;
                LocalAnchorB = anchor;
            }
        }

        public FrictionJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, bool useWorldCoordinates = false)
            : base(bodyA.Owner, bodyB.Owner)
        {
            if (useWorldCoordinates)
            {
                LocalAnchorA = BodyA.GetLocalPoint(Vector2.Zero);
                LocalAnchorB = BodyB.GetLocalPoint(Vector2.Zero);
            }
            else
            {
                LocalAnchorA = Vector2.Zero;
                LocalAnchorB = Vector2.Zero;
            }
        }

        public override JointState GetState()
        {
            var frictionState = new FrictionJointState();

            base.GetState(frictionState);
            return frictionState;
        }

        internal override void ApplyState(JointState state)
        {
            base.ApplyState(state);
            if (state is not FrictionJointState frictionState) return;

            MaxForce = frictionState.MaxForce;
            MaxTorque = frictionState.MaxTorque;
        }

        public override Vector2 GetReactionForce(float invDt)
        {
            return _linearImpulse * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _angularImpulse;
        }

        internal override void InitVelocityConstraints(SolverData data)
        {
            _indexA = BodyA.IslandIndex[data.IslandIndex];
            _indexB = BodyB.IslandIndex[data.IslandIndex];
            _localCenterA = BodyA.LocalCenter;
            _localCenterB = BodyB.LocalCenter;
            _invMassA = BodyA.InvMass;
            _invMassB = BodyB.InvMass;
            _invIA = BodyA.InvI;
            _invIB = BodyB.InvI;

            float aA = data.Angles[_indexA];
            Vector2 vA = data.LinearVelocities[_indexA];
            float wA = data.AngularVelocities[_indexA];

            float aB = data.Angles[_indexB];
            Vector2 vB = data.LinearVelocities[_indexB];
            float wB = data.AngularVelocities[_indexB];

            Quaternion2D qA = new(aA), qB = new(aB);

            // Compute the effective mass matrix.
            _rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
            _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
            //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
            //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            Span<Vector2> K = stackalloc Vector2[2];
            K[0].X = mA + mB + iA * _rA.Y * _rA.Y + iB * _rB.Y * _rB.Y;
            K[0].Y = -iA * _rA.X * _rA.Y - iB * _rB.X * _rB.Y;
            K[1].X = K[0].Y;
            K[1].Y = mA + mB + iA * _rA.X * _rA.X + iB * _rB.X * _rB.X;

            _linearMass = Vector2Helpers.Inverse(K);

            _angularMass = iA + iB;
            if (_angularMass > 0.0f)
            {
                _angularMass = 1.0f / _angularMass;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.WarmStarting))
            {
                // Scale impulses to support a variable time step.
                _linearImpulse *= data.DtRatio;
                _angularImpulse *= data.DtRatio;

                Vector2 P = new Vector2(_linearImpulse.X, _linearImpulse.Y);
                vA -= P * mA;
                wA -= iA * (Vector2.Cross(_rA, P) + _angularImpulse);
                vB += P * mB;
                wB += iB * (Vector2.Cross(_rB, P) + _angularImpulse);
            }
            else
            {
                _linearImpulse = Vector2.Zero;
                _angularImpulse = 0.0f;
            }

            data.LinearVelocities[_indexA] = vA;
            data.AngularVelocities[_indexA] = wA;
            data.LinearVelocities[_indexB] = vB;
            data.AngularVelocities[_indexB] = wB;
        }

        internal override void SolveVelocityConstraints(SolverData data)
        {
            Vector2 vA = data.LinearVelocities[_indexA];
            float wA = data.AngularVelocities[_indexA];
            Vector2 vB = data.LinearVelocities[_indexB];
            float wB = data.AngularVelocities[_indexB];

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            float h = data.FrameTime;

            // Solve angular friction
            {
                float Cdot = wB - wA;
                float impulse = -_angularMass * Cdot;

                float oldImpulse = _angularImpulse;
                float maxImpulse = h * MaxTorque;
                _angularImpulse = Math.Clamp(_angularImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _angularImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve linear friction
            {
                Vector2 Cdot = vB + Vector2.Cross(wB, _rB) - vA - Vector2.Cross(wA, _rA);

                Vector2 impulse = -Transform.Mul(_linearMass, Cdot);
                Vector2 oldImpulse = _linearImpulse;
                _linearImpulse += impulse;

                float maxImpulse = h * MaxForce;

                if (_linearImpulse.LengthSquared > maxImpulse * maxImpulse)
                {
                    _linearImpulse = _linearImpulse.Normalized;
                    _linearImpulse *= maxImpulse;
                }

                impulse = _linearImpulse - oldImpulse;

                vA -= impulse * mA;
                wA -= iA * Vector2.Cross(_rA, impulse);

                vB += impulse * mB;
                wB += iB * Vector2.Cross(_rB, impulse);
            }

            data.LinearVelocities[_indexA] = vA;
            data.AngularVelocities[_indexA] = wA;
            data.LinearVelocities[_indexB] = vB;
            data.AngularVelocities[_indexB] = wB;
        }

        internal override bool SolvePositionConstraints(SolverData data)
        {
            return true;
        }

        public bool Equals(FrictionJoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!base.Equals(other)) return false;

            return MathHelper.CloseTo(MaxForce, other.MaxForce) &&
                   MathHelper.CloseTo(MaxTorque, other.MaxTorque);
        }
    }
}
