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
        /// The local translation unit axis in bodyA.
        public Vector2 LocalAxisA;

        /// The constrained angle between the bodies: bodyB_angle - bodyA_angle.
        public float ReferenceAngle;

        /// Enable/disable the joint limit.
        public bool EnableLimit;

        /// The lower translation limit, usually in meters.
        public float LowerTranslation;

        /// The upper translation limit, usually in meters.
        public float UpperTranslation;

        /// Enable/disable the joint motor.
        public bool EnableMotor;

        /// The maximum motor torque, usually in N-m.
        public float MaxMotorForce;

        /// The desired motor speed in radians per second.
        public float MotorSpeed;

        public override Joint GetJoint()
        {
            var joint = new PrismaticJoint(UidA, UidB)
            {
                ID = ID,
                Breakpoint = Breakpoint,
                CollideConnected = CollideConnected,
                Enabled = Enabled,
                LocalAxisA = LocalAxisA,
                ReferenceAngle = ReferenceAngle,
                EnableLimit = EnableLimit,
                LowerTranslation = LowerTranslation,
                UpperTranslation = UpperTranslation,
                EnableMotor = EnableMotor,
                MaxMotorForce = MaxMotorForce,
                MotorSpeed = MotorSpeed,
                LocalAnchorA = LocalAnchorA,
                LocalAnchorB = LocalAnchorB
            };

            return joint;
        }

    }

    /// <summary>
    /// Prismatic joint definition. This requires defining a line of
    /// motion using an axis and an anchor point. The definition uses local
    /// anchor points and a local axis so that the initial configuration
    /// can violate the constraint slightly. The joint translation is zero
    /// when the local anchor points coincide in world space.
    /// </summary>
    public sealed class PrismaticJoint : Joint, IEquatable<PrismaticJoint>
    {
        /// The local translation unit axis in bodyA.
        public Vector2 LocalAxisA
        {
            get => _localAxisA;
            set
            {
                _localAxisA = value;
                _localXAxisA = value.Normalized;
                _localYAxisA = Vector2.Cross(1f, _localXAxisA);
            }
        }

        private Vector2 _localAxisA;

        /// The constrained angle between the bodies: bodyB_angle - bodyA_angle.
        public float ReferenceAngle;

        /// Enable/disable the joint limit.
        public bool EnableLimit;

        /// The lower translation limit, usually in meters.
        public float LowerTranslation;

        /// The upper translation limit, usually in meters.
        public float UpperTranslation;

        /// Enable/disable the joint motor.
        public bool EnableMotor;

        /// The maximum motor torque, usually in N-m.
        public float MaxMotorForce;

        /// The desired motor speed in radians per second.
        public float MotorSpeed;

        internal Vector2 _localXAxisA;
        internal Vector2 _localYAxisA;

        private Vector2 _impulse;
        private float _motorImpulse;
        private float _lowerImpulse;
        private float _upperImpulse;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private Vector2 _axis, _perp;
        private float _s1, _s2;
        private float _a1, _a2;
        Matrix22 _K;
        private float _translation;
        private float _axialMass;

        public PrismaticJoint() {}

        public PrismaticJoint(EntityUid bodyAUid, EntityUid bodyBUid) : base(bodyAUid, bodyBUid)
        {
            LocalAxisA = new Vector2(1f, 0f);
        }

        public PrismaticJoint(EntityUid bodyAUid, EntityUid bodyBUid, Vector2 anchor, Vector2 axis, IEntityManager entityManager) : base(bodyAUid, bodyBUid)
        {
            var xformA = entityManager.GetComponent<TransformComponent>(bodyAUid);
            var xformB = entityManager.GetComponent<TransformComponent>(bodyBUid);

            LocalAnchorA = xformA.InvWorldMatrix.Transform(anchor);
            LocalAnchorB = xformB.InvWorldMatrix.Transform(anchor);
            LocalAxisA = entityManager.GetComponent<PhysicsComponent>(bodyAUid).GetLocalVector2(axis);
            ReferenceAngle = (float) (xformB.WorldRotation - xformA.WorldRotation);
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
            return (_perp * _impulse.X + _axis * (_motorImpulse + _lowerImpulse - _upperImpulse)) * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _impulse.Y;
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

	        var cA = data.Positions[_indexA];
	        float aA = data.Angles[_indexA];
	        var vA = data.Positions[_indexA];
	        float wA = data.Angles[_indexA];

	        var cB = data.Positions[_indexB];
	        float aB = data.Angles[_indexB];
	        var vB = data.Positions[_indexB];
	        float wB = data.Angles[_indexB];

	        Quaternion2D qA = new(aA), qB = new(aB);

	        // Compute the effective masses.
	        var rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
            var rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
            var d = (cB - cA) + rB - rA;

	        float mA = _invMassA, mB = _invMassB;
	        float iA = _invIA, iB = _invIB;

	        // Compute motor Jacobian and effective mass.
	        {
		        _axis = Transform.Mul(qA, _localXAxisA);
		        _a1 = Vector2.Cross(d + rA, _axis);
		        _a2 = Vector2.Cross(rB, _axis);

		        _axialMass = mA + mB + iA * _a1 * _a1 + iB * _a2 * _a2;
		        if (_axialMass > 0.0f)
		        {
			        _axialMass = 1.0f / _axialMass;
		        }
	        }

	        // Prismatic constraint.
	        {
		        _perp = Transform.Mul(qA, _localYAxisA);

		        _s1 = Vector2.Cross(d + rA, _perp);
		        _s2 = Vector2.Cross(rB, _perp);

		        float k11 = mA + mB + iA * _s1 * _s1 + iB * _s2 * _s2;
		        float k12 = iA * _s1 + iB * _s2;
		        float k22 = iA + iB;
		        if (k22 == 0.0f)
		        {
			        // For bodies with fixed rotation.
			        k22 = 1.0f;
		        }

                _K = new Matrix22(new Vector2(k11, k12), new Vector2(k12, k22));
            }

	        if (EnableLimit)
	        {
		        _translation = Vector2.Dot(_axis, d);
	        }
	        else
	        {
		        _lowerImpulse = 0.0f;
		        _upperImpulse = 0.0f;
	        }

	        if (!EnableMotor)
	        {
		        _motorImpulse = 0.0f;
	        }

	        if (data.WarmStarting)
	        {
		        // Account for variable time step.
		        _impulse *= data.DtRatio;
		        _motorImpulse *= data.DtRatio;
		        _lowerImpulse *= data.DtRatio;
		        _upperImpulse *= data.DtRatio;

		        float axialImpulse = _motorImpulse + _lowerImpulse - _upperImpulse;
		        Vector2 P = _perp * _impulse.X + _axis * axialImpulse;
		        float LA = _impulse.X * _s1 + _impulse.Y + axialImpulse * _a1;
		        float LB = _impulse.X * _s2 + _impulse.Y + axialImpulse * _a2;

		        vA -= P * mA;
		        wA -= iA * LA;

		        vB += P * mB;
		        wB += iB * LB;
	        }
	        else
	        {
		        _impulse = Vector2.Zero;
		        _motorImpulse = 0.0f;
		        _lowerImpulse = 0.0f;
		        _upperImpulse = 0.0f;
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

	        // Solve linear motor constraint
	        if (EnableMotor)
	        {
		        float Cdot = Vector2.Dot(_axis, vB - vA) + _a2 * wB - _a1 * wA;
		        float impulse = _axialMass * (MotorSpeed - Cdot);
		        float oldImpulse = _motorImpulse;
		        float maxImpulse = data.FrameTime * MaxMotorForce;
		        _motorImpulse = Math.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
		        impulse = _motorImpulse - oldImpulse;

		        Vector2 P = _axis * impulse;
		        float LA = impulse * _a1;
		        float LB = impulse * _a2;

		        vA -= P * mA;
		        wA -= iA * LA;
		        vB += P * mB;
		        wB += iB * LB;
	        }

	        if (EnableLimit)
	        {
		        // Lower limit
		        {
			        float C = _translation - LowerTranslation;
			        float Cdot = Vector2.Dot(_axis, vB - vA) + _a2 * wB - _a1 * wA;
			        float impulse = -_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.InvDt);
			        float oldImpulse = _lowerImpulse;
			        _lowerImpulse = MathF.Max(_lowerImpulse + impulse, 0.0f);
			        impulse = _lowerImpulse - oldImpulse;

			        Vector2 P = _axis * impulse;
			        float LA = impulse * _a1;
			        float LB = impulse * _a2;

			        vA -= P * mA;
			        wA -= iA * LA;
			        vB += P * mB;
			        wB += iB * LB;
		        }

		        // Upper limit
		        // Note: signs are flipped to keep C positive when the constraint is satisfied.
		        // This also keeps the impulse positive when the limit is active.
		        {
			        float C = UpperTranslation - _translation;
			        float Cdot = Vector2.Dot(_axis, vA - vB) + _a1 * wA - _a2 * wB;
			        float impulse = -_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.InvDt);
			        float oldImpulse = _upperImpulse;
			        _upperImpulse = MathF.Max(_upperImpulse + impulse, 0.0f);
			        impulse = _upperImpulse - oldImpulse;

			        Vector2 P = _axis * impulse;
			        float LA = impulse * _a1;
			        float LB = impulse * _a2;

			        vA += P * mA;
			        wA += iA * LA;
			        vB -= P * mB;
			        wB -= iB * LB;
		        }
	        }

	        // Solve the prismatic constraint in block form.
	        {
		        Vector2 Cdot;
		        Cdot.X = Vector2.Dot(_perp, vB - vA) + _s2 * wB - _s1 * wA;
		        Cdot.Y = wB - wA;

		        Vector2 df = _K.Solve(-Cdot);
		        _impulse += df;

		        Vector2 P = _perp * df.X;
		        float LA = df.X * _s1 + df.Y;
		        float LB = df.X * _s2 + df.Y;

		        vA -= P * mA;
		        wA -= iA * LA;

		        vB += P * mB;
		        wB += iB * LB;
	        }

	        data.LinearVelocities[_indexA] = vA;
	        data.AngularVelocities[_indexA] = wA;
	        data.LinearVelocities[_indexB] = vB;
	        data.AngularVelocities[_indexB] = wB;
        }

        internal override bool SolvePositionConstraints(SolverData data)
        {
            Vector2 cA = data.Positions[_indexA];
	        float aA = data.Angles[_indexA];
	        Vector2 cB = data.Positions[_indexB];
	        float aB = data.Angles[_indexB];

	        Quaternion2D qA = new(aA), qB = new(aB);

	        float mA = _invMassA, mB = _invMassB;
	        float iA = _invIA, iB = _invIB;

	        // Compute fresh Jacobians
	        Vector2 rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
	        Vector2 rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
	        Vector2 d = cB + rB - cA - rA;

	        Vector2 axis = Transform.Mul(qA, _localXAxisA);
	        float a1 = Vector2.Cross(d + rA, axis);
	        float a2 = Vector2.Cross(rB, axis);
	        Vector2 perp = Transform.Mul(qA, _localYAxisA);

	        float s1 = Vector2.Cross(d + rA, perp);
	        float s2 = Vector2.Cross(rB, perp);

	        Vector3 impulse;
	        Vector2 C1;
	        C1.X = Vector2.Dot(perp, d);
	        C1.Y = aB - aA - ReferenceAngle;

	        float linearError = MathF.Abs(C1.X);
	        float angularError = MathF.Abs(C1.Y);

	        bool active = false;
	        float C2 = 0.0f;
	        if (EnableLimit)
	        {
		        float translation = Vector2.Dot(axis, d);
		        if (MathF.Abs(UpperTranslation - LowerTranslation) < 2.0f * data.LinearSlop)
		        {
			        C2 = translation;
			        linearError = MathF.Max(linearError, MathF.Abs(translation));
			        active = true;
		        }
		        else if (translation <= LowerTranslation)
		        {
			        C2 = MathF.Min(translation - LowerTranslation, 0.0f);
			        linearError = MathF.Max(linearError, LowerTranslation - translation);
			        active = true;
		        }
		        else if (translation >= UpperTranslation)
		        {
			        C2 = MathF.Max(translation - UpperTranslation, 0.0f);
			        linearError = MathF.Max(linearError, translation - UpperTranslation);
			        active = true;
		        }
	        }

	        if (active)
	        {
		        float k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
		        float k12 = iA * s1 + iB * s2;
		        float k13 = iA * s1 * a1 + iB * s2 * a2;
		        float k22 = iA + iB;
		        if (k22 == 0.0f)
		        {
			        // For fixed rotation
			        k22 = 1.0f;
		        }
		        float k23 = iA * a1 + iB * a2;
		        float k33 = mA + mB + iA * a1 * a1 + iB * a2 * a2;

                Matrix33 K = new Matrix33(
                    new Vector3(k11, k12, k13),
                    new Vector3(k12, k22, k23),
                    new Vector3(k13, k23, k33));

                Vector3 C;
		        C.X = C1.X;
		        C.Y = C1.Y;
		        C.Z = C2;

		        impulse = K.Solve33(-C);
	        }
	        else
	        {
		        float k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
		        float k12 = iA * s1 + iB * s2;
		        float k22 = iA + iB;
		        if (k22 == 0.0f)
		        {
			        k22 = 1.0f;
		        }

		        Matrix22 K;
                K = new Matrix22(k11, k12, k12, k22);

		        Vector2 impulse1 = K.Solve(-C1);
		        impulse.X = impulse1.X;
		        impulse.Y = impulse1.Y;
		        impulse.Z = 0.0f;
	        }

	        Vector2 P = perp * impulse.X + axis * impulse.Z;
	        float LA = impulse.X * s1 + impulse.Y + impulse.Z * a1;
	        float LB = impulse.X * s2 + impulse.Y + impulse.Z * a2;

	        cA -= P * mA;
	        aA -= iA * LA;
	        cB += P * mB;
	        aB += iB * LB;

	        data.Positions[_indexA] = cA;
	        data.Angles[_indexA] = aA;
	        data.Positions[_indexB] = cB;
	        data.Angles[_indexB] = aB;

	        return linearError <= data.LinearSlop && angularError <= data.AngularSlop;
        }

        public bool Equals(PrismaticJoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!base.Equals(other)) return false;

            return EnableLimit.Equals(other.EnableLimit) &&
                   EnableMotor.Equals(other.EnableMotor) &&
                   LocalAxisA.EqualsApprox(other.LocalAxisA) &&
                   MathHelper.CloseTo(ReferenceAngle, other.ReferenceAngle) &&
                   MathHelper.CloseTo(LowerTranslation, other.LowerTranslation) &&
                   MathHelper.CloseTo(UpperTranslation, other.UpperTranslation) &&
                   MathHelper.CloseTo(MaxMotorForce, other.MaxMotorForce) &&
                   MathHelper.CloseTo(MotorSpeed, other.MotorSpeed);
        }
    }
}
