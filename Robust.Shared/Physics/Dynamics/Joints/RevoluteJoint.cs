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
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    [Serializable, NetSerializable]
    public class RevoluteJoint : Joint
    {
        [NonSerialized] private Vector2 _impulse;
        [NonSerialized] private int _indexA;
        [NonSerialized] private int _indexB;
        [NonSerialized] private Vector2 _localCenterA;
        [NonSerialized] private Vector2 _localCenterB;
        [NonSerialized] private float _invMassA;
        [NonSerialized] private float _invMassB;
        [NonSerialized] private float _invIA;
        [NonSerialized] private float _invIB;
        [NonSerialized] private Vector2 _rA;
        [NonSerialized] private Vector2 _rB;
        [NonSerialized] private Matrix22 _K;
        [NonSerialized] private float _axialMass;
        [NonSerialized] private float _angle;
        [NonSerialized] private float _motorImpulse;
        [NonSerialized] private float _lowerImpulse;
        [NonSerialized] private float _upperImpulse;

        public Vector2 LocalAnchorA;
        public Vector2 LocalAnchorB;

        public bool EnableLimit;
        public bool EnableMotor;
        public float ReferenceAngle;
        public float LowerAngle;
        public float UpperAngle;
        public float MotorSpeed;
        public float MaxMotorTorque;

        public RevoluteJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchor) : base(bodyA, bodyB)
        {
            LocalAnchorA = bodyA.GetLocalPoint(anchor);
            LocalAnchorB = bodyB.GetLocalPoint(anchor);
            ReferenceAngle = (float) (bodyB.Owner.Transform.WorldRotation - bodyA.Owner.Transform.WorldRotation).Theta;
        }

        public RevoluteJoint(PhysicsComponent bodyA, PhysicsComponent bodyB) : base(bodyA, bodyB) {}

        public override JointType JointType => JointType.Revolute;

        public override Vector2 WorldAnchorA { get; set; }
        public override Vector2 WorldAnchorB { get; set; }

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
            _localCenterA = Vector2.Zero; //BodyA->m_sweep.localCenter;
            _localCenterB = Vector2.Zero; //BodyB->m_sweep.localCenter;
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
    }
}
