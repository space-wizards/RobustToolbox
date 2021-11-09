// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    // Linear constraint (point-to-line)
// d = p2 - p1 = x2 + r2 - x1 - r1
// C = dot(perp, d)
// Cdot = dot(d, cross(w1, perp)) + dot(perp, v2 + cross(w2, r2) - v1 - cross(w1, r1))
//      = -dot(perp, v1) - dot(cross(d + r1, perp), w1) + dot(perp, v2) + dot(cross(r2, perp), v2)
// J = [-perp, -cross(d + r1, perp), perp, cross(r2,perp)]
//
// Angular constraint
// C = a2 - a1 + a_initial
// Cdot = w2 - w1
// J = [0 0 -1 0 0 1]
//
// K = J * invM * JT
//
// J = [-a -s1 a s2]
//     [0  -1  0  1]
// a = perp
// s1 = cross(d + r1, a) = cross(p2 - x1, a)
// s2 = cross(r2, a) = cross(p2 - x2, a)

// Motor/Limit linear constraint
// C = dot(ax1, d)
// Cdot = -dot(ax1, v1) - dot(cross(d + r1, ax1), w1) + dot(ax1, v2) + dot(cross(r2, ax1), v2)
// J = [-ax1 -cross(d+r1,ax1) ax1 cross(r2,ax1)]

// Predictive limit is applied even when the limit is not active.
// Prevents a constraint speed that can lead to a constraint error in one time step.
// Want C2 = C1 + h * Cdot >= 0
// Or:
// Cdot + C1/h >= 0
// I do not apply a negative constraint error because that is handled in position correction.
// So:
// Cdot + max(C1, 0)/h >= 0

// Block Solver
// We develop a block solver that includes the angular and linear constraints. This makes the limit stiffer.
//
// The Jacobian has 2 rows:
// J = [-uT -s1 uT s2] // linear
//     [0   -1   0  1] // angular
//
// u = perp
// s1 = cross(d + r1, u), s2 = cross(r2, u)
// a1 = cross(d + r1, v), a2 = cross(r2, v)

    [Serializable, NetSerializable]
    internal sealed class PrismaticJointState : JointState
    {


        public override Joint GetJoint()
        {
            var joint = new PrismaticJoint(UidA, UidB, LocalAnchorA, LocalAnchorB)
            {
                ID = ID,
                Breakpoint = Breakpoint,
                CollideConnected = CollideConnected,
                Enabled = Enabled,
                Damping = Damping,
                Length = Length,
                MinLength = MinLength,
                MaxLength = MaxLength,
                Stiffness = Stiffness,
                LocalAnchorA = LocalAnchorA,
                LocalAnchorB = LocalAnchorB
            };

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            joint.LinearSlop = configManager.GetCVar(CVars.LinearSlop);
            joint.WarmStarting = configManager.GetCVar(CVars.WarmStarting);

            return joint;
        }

    }

    public sealed class PrismaticJoint : Joint, IEquatable<PrismaticJoint>
    {
        prim_localAnchorA = def->localAnchorA;
        m_localAnchorB = def->localAnchorB;
        m_localXAxisA = def->localAxisA;
        m_localXAxisA.Normalize();
        m_localYAxisA = b2Cross(1.0f, m_localXAxisA);
        m_referenceAngle = def->referenceAngle;

        m_impulse.SetZero();
        m_axialMass = 0.0f;
        m_motorImpulse = 0.0f;
        m_lowerImpulse = 0.0f;
        m_upperImpulse = 0.0f;

        m_lowerTranslation = def->lowerTranslation;
        m_upperTranslation = def->upperTranslation;

        b2Assert(m_lowerTranslation <= m_upperTranslation);

        m_maxMotorForce = def->maxMotorForce;
        m_motorSpeed = def->motorSpeed;
        m_enableLimit = def->enableLimit;
        m_enableMotor = def->enableMotor;

        m_translation = 0.0f;
        m_axis.SetZero();
        m_perp.SetZero();

        public PrismaticJoint(EntityUid bodyAUid, EntityUid bodyBUid) : base(bodyAUid, bodyBUid)
        {
        }

        public override JointType JointType => JointType.Prismatic;

        public override JointState GetState()
        {
            var prismaticState = new PrismaticJointState
            {
                LocalAnchorA = LocalAnchorA,
                LocalAnchorB = LocalAnchorB
            };

            base.GetState(prismaticState);
            return prismaticState;
        }

        public override Vector2 GetReactionForce(float invDt)
        {
            return invDt * (_impulse.x * _perp + (_motorImpulse + _lowerImpulse - _upperImpulse) * _axis);
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _impulse.y;
        }

        internal override void InitVelocityConstraints(SolverData data)
        {
            throw new NotImplementedException();
        }

        internal override void SolveVelocityConstraints(SolverData data)
        {
            throw new NotImplementedException();
        }

        internal override bool SolvePositionConstraints(SolverData data)
        {
            throw new NotImplementedException();
        }

        public bool Equals(PrismaticJoint? other)
        {
            throw new NotImplementedException();
        }
    }
}
