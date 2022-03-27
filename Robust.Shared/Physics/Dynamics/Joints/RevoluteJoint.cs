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
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    [Serializable, NetSerializable]
    internal sealed class RevoluteJointState : JointState
    {
        public bool EnableLimit { get; internal set; }
        public bool EnableMotor { get; internal set; }
        public float ReferenceAngle { get; internal set; }
        public float LowerAngle { get; internal set; }
        public float UpperAngle { get; internal set; }
        public float MotorSpeed { get; internal set; }
        public float MaxMotorTorque { get; internal set; }

        public override Joint GetJoint()
        {
            var joint = new RevoluteJoint(UidA, UidB)
            {
                ID = ID,
                Breakpoint = Breakpoint,
                CollideConnected = CollideConnected,
                Enabled = Enabled,
                EnableLimit = EnableLimit,
                EnableMotor = EnableMotor,
                ReferenceAngle = ReferenceAngle,
                LowerAngle = LowerAngle,
                UpperAngle = UpperAngle,
                MotorSpeed = MotorSpeed,
                MaxMotorTorque = MaxMotorTorque,
                LocalAnchorA = LocalAnchorA,
                LocalAnchorB = LocalAnchorB
            };

            return joint;
        }
    }

    public sealed class RevoluteJoint : Joint, IEquatable<RevoluteJoint>
    {
        // Temporary
        private Vector2 _impulse;
        private int _indexA;
        private int _indexB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private Vector2 _rA;
        private Vector2 _rB;
        private Matrix22 _K;
        private float _axialMass;
        private float _angle;
        private float _motorImpulse;
        private float _lowerImpulse;
        private float _upperImpulse;

        // Settable
        public bool EnableLimit;

        /// <summary>
        /// A flag to enable the joint motor.
        /// </summary>
        public bool EnableMotor;

        /// <summary>
        /// The bodyB angle minus bodyA angle in the reference state (radians).
        /// </summary>
        public float ReferenceAngle;

        /// <summary>
        /// The lower angle for the joint limit (radians).
        /// </summary>
        public float LowerAngle;

        /// <summary>
        /// The upper angle for the joint limit (radians).
        /// </summary>
        public float UpperAngle;

        /// <summary>
        /// The desired motor speed. Usually in radians per second.
        /// </summary>
        public float MotorSpeed;

        /// <summary>
        /// The maximum motor torque used to achieve the desired motor speed.
        /// Usually in N-m.
        /// </summary>
        public float MaxMotorTorque;

        public RevoluteJoint() {}

        public RevoluteJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchor) : base(bodyA.Owner, bodyB.Owner)
        {
            LocalAnchorA = bodyA.GetLocalPoint(anchor);
            LocalAnchorB = bodyB.GetLocalPoint(anchor);
            ReferenceAngle = (float) (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(bodyB.Owner).WorldRotation - IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(bodyA.Owner).WorldRotation).Theta;
        }

        public RevoluteJoint(EntityUid bodyAUid, EntityUid bodyBUid) : base(bodyAUid, bodyBUid) {}

        public override JointType JointType => JointType.Revolute;

        public override JointState GetState()
        {
            var revoluteState = new RevoluteJointState();

            base.GetState(revoluteState);
            return revoluteState;
        }

        internal override void ApplyState(JointState state)
        {
            base.ApplyState(state);
            if (state is not RevoluteJointState revoluteState) return;

            EnableLimit = revoluteState.EnableLimit;
            EnableMotor = revoluteState.EnableMotor;
            LowerAngle = revoluteState.LowerAngle;
            MotorSpeed = revoluteState.MotorSpeed;
            ReferenceAngle = revoluteState.ReferenceAngle;
            UpperAngle = revoluteState.UpperAngle;
            MaxMotorTorque = revoluteState.MaxMotorTorque;
        }

        public override Vector2 GetReactionForce(float invDt)
        {
            var P = new Vector2(_impulse.X, _impulse.Y);
            return P * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * (_motorImpulse + _lowerImpulse - _upperImpulse);
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
	        var vA = data.LinearVelocities[_indexA];
	        float wA = data.AngularVelocities[_indexA];

	        float aB = data.Angles[_indexB];
	        var vB = data.LinearVelocities[_indexB];
	        float wB = data.AngularVelocities[_indexB];

	        Quaternion2D qA = new(aA), qB = new(aB);

	        _rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
	        _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

	        // J = [-I -r1_skew I r2_skew]
	        // r_skew = [-ry; rx]

	        // Matlab
	        // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x]
	        //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB]

	        float mA = _invMassA, mB = _invMassB;
	        float iA = _invIA, iB = _invIB;

            _K.EX.X = mA + mB + _rA.Y * _rA.Y * iA + _rB.Y * _rB.Y * iB;
	        _K.EY.X = -_rA.Y * _rA.X * iA - _rB.Y * _rB.X * iB;
	        _K.EX.Y = _K.EY.X;
	        _K.EY.Y = mA + mB + _rA.X * _rA.X * iA + _rB.X * _rB.X * iB;

	        _axialMass = iA + iB;
	        bool fixedRotation;
	        if (_axialMass > 0.0f)
	        {
		        _axialMass = 1.0f / _axialMass;
		        fixedRotation = false;
	        }
	        else
	        {
		        fixedRotation = true;
	        }

	        _angle = aB - aA - ReferenceAngle;
	        if (EnableLimit == false || fixedRotation)
	        {
		        _lowerImpulse = 0.0f;
		        _upperImpulse = 0.0f;
	        }

	        if (EnableMotor == false || fixedRotation)
	        {
		        _motorImpulse = 0.0f;
	        }

	        if (data.WarmStarting)
	        {
		        // Scale impulses to support a variable time step.
		        _impulse *= data.DtRatio;
		        _motorImpulse *= data.DtRatio;
		        _lowerImpulse *= data.DtRatio;
		        _upperImpulse *= data.DtRatio;

		        float axialImpulse = _motorImpulse + _lowerImpulse - _upperImpulse;
		        var P = new Vector2(_impulse.X, _impulse.Y);

		        vA -= P * mA;
		        wA -= iA * (Vector2.Cross(_rA, P) + axialImpulse);

		        vB += P * mB;
		        wB += iB * (Vector2.Cross(_rB, P) + axialImpulse);
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
            var vA = data.LinearVelocities[_indexA];
	        float wA = data.AngularVelocities[_indexA];
	        var vB = data.LinearVelocities[_indexB];
	        float wB = data.AngularVelocities[_indexB];

	        float mA = _invMassA, mB = _invMassB;
	        float iA = _invIA, iB = _invIB;

	        bool fixedRotation = (iA + iB == 0.0f);

	        // Solve motor constraint.
	        if (EnableMotor && !fixedRotation)
	        {
		        float Cdot = wB - wA - MotorSpeed;
		        float impulse = -_axialMass * Cdot;
		        float oldImpulse = _motorImpulse;
		        float maxImpulse = data.FrameTime * MaxMotorTorque;
		        _motorImpulse = Math.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
		        impulse = _motorImpulse - oldImpulse;

		        wA -= iA * impulse;
		        wB += iB * impulse;
	        }

	        if (EnableLimit && fixedRotation == false)
	        {
		        // Lower limit
		        {
			        float C = _angle - LowerAngle;
			        float Cdot = wB - wA;
			        float impulse = -_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.InvDt);
			        float oldImpulse = _lowerImpulse;
			        _lowerImpulse = MathF.Max(_lowerImpulse + impulse, 0.0f);
			        impulse = _lowerImpulse - oldImpulse;

			        wA -= iA * impulse;
			        wB += iB * impulse;
		        }

		        // Upper limit
		        // Note: signs are flipped to keep C positive when the constraint is satisfied.
		        // This also keeps the impulse positive when the limit is active.
		        {
			        float C = UpperAngle - _angle;
			        float Cdot = wA - wB;
			        float impulse = -_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.InvDt);
			        float oldImpulse = _upperImpulse;
			        _upperImpulse = MathF.Max(_upperImpulse + impulse, 0.0f);
			        impulse = _upperImpulse - oldImpulse;

			        wA += iA * impulse;
			        wB -= iB * impulse;
		        }
	        }

	        // Solve point-to-point constraint
	        {
		        var Cdot = vB + Vector2.Cross(wB, _rB) - vA - Vector2.Cross(wA, _rA);
		        var impulse = _K.Solve(-Cdot);

		        _impulse.X += impulse.X;
		        _impulse.Y += impulse.Y;

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
            var cA = data.Positions[_indexA];
	        float aA = data.Angles[_indexA];
	        var cB = data.Positions[_indexB];
	        float aB = data.Angles[_indexB];

	        Quaternion2D qA = new(aA), qB = new(aB);

	        float angularError = 0.0f;
	        float positionError = 0.0f;

	        bool fixedRotation = (_invIA + _invIB == 0.0f);

	        // Solve angular limit constraint
	        if (EnableLimit && fixedRotation == false)
	        {
		        float angle = aB - aA - ReferenceAngle;
		        float C = 0.0f;

		        if (Math.Abs(UpperAngle - LowerAngle) < 2.0f * data.AngularSlop)
		        {
			        // Prevent large angular corrections
			        C = Math.Clamp(angle - LowerAngle, -data.MaxAngularCorrection, data.MaxAngularCorrection);
		        }
		        else if (angle <= LowerAngle)
		        {
			        // Prevent large angular corrections and allow some slop.
			        C = Math.Clamp(angle - LowerAngle + data.AngularSlop, -data.MaxAngularCorrection, 0.0f);
		        }
		        else if (angle >= UpperAngle)
		        {
			        // Prevent large angular corrections and allow some slop.
			        C = Math.Clamp(angle - UpperAngle - data.AngularSlop, 0.0f, data.MaxAngularCorrection);
		        }

		        float limitImpulse = -_axialMass * C;
		        aA -= _invIA * limitImpulse;
		        aB += _invIB * limitImpulse;
		        angularError = Math.Abs(C);
	        }

	        // Solve point-to-point constraint.
	        {
		        qA.Set(aA);
		        qB.Set(aB);
		        var rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
		        var rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

		        var C = cB + rB - cA - rA;
		        positionError = C.Length;

		        float mA = _invMassA, mB = _invMassB;
		        float iA = _invIA, iB = _invIB;

		        var K = new Matrix22(
                    mA + mB + iA * rA.Y * rA.Y + iB * rB.Y * rB.Y,
                    -iA * rA.X * rA.Y - iB * rB.X * rB.Y,
                    0f,
                    mA + mB + iA * rA.X * rA.X + iB * rB.X * rB.X);

                K.EY.X = K.EX.Y;

                var impulse = -K.Solve(C);

		        cA -= impulse * mA;
		        aA -= iA * Vector2.Cross(rA, impulse);

		        cB += impulse * mB;
		        aB += iB * Vector2.Cross(rB, impulse);
	        }

	        data.Positions[_indexA] = cA;
	        data.Angles[_indexA] = aA;
	        data.Positions[_indexB] = cB;
	        data.Angles[_indexB] = aB;

	        return positionError <= data.LinearSlop && angularError <= data.AngularSlop;
        }

        public bool Equals(RevoluteJoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!base.Equals(other)) return false;

            return EnableLimit == other.EnableLimit &&
                   EnableMotor == other.EnableMotor &&
                   MathHelper.CloseTo(ReferenceAngle, other.ReferenceAngle) &&
                   MathHelper.CloseTo(LowerAngle, other.LowerAngle) &&
                   MathHelper.CloseTo(UpperAngle, other.UpperAngle) &&
                   MathHelper.CloseTo(MotorSpeed, other.MotorSpeed) &&
                   MathHelper.CloseTo(MaxMotorTorque, other.MaxMotorTorque);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != GetType()) return false;
            return Equals((RevoluteJoint) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(_impulse);
            hashCode.Add(_indexA);
            hashCode.Add(_indexB);
            hashCode.Add(_localCenterA);
            hashCode.Add(_localCenterB);
            hashCode.Add(_invMassA);
            hashCode.Add(_invMassB);
            hashCode.Add(_invIA);
            hashCode.Add(_invIB);
            hashCode.Add(_rA);
            hashCode.Add(_rB);
            hashCode.Add(_K);
            hashCode.Add(_axialMass);
            hashCode.Add(_angle);
            hashCode.Add(_motorImpulse);
            hashCode.Add(_lowerImpulse);
            hashCode.Add(_upperImpulse);
            hashCode.Add(EnableLimit);
            hashCode.Add(EnableMotor);
            hashCode.Add(ReferenceAngle);
            hashCode.Add(LowerAngle);
            hashCode.Add(UpperAngle);
            hashCode.Add(MotorSpeed);
            hashCode.Add(MaxMotorTorque);
            return hashCode.ToHashCode();
        }
    }
}
