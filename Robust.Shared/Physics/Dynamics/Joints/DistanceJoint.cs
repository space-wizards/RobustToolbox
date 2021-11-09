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

/*
 * Farseer DistanceJoint but with some recent Box2D additions
 */

using System;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    [Serializable, NetSerializable]
    internal sealed class DistanceJointState : JointState
    {
        public float Length { get; internal set; }
        public float MinLength { get; internal set; }
        public float MaxLength { get; internal set; }
        public float Stiffness { get; internal set; }
        public float Damping { get; internal set; }

        public override Joint GetJoint()
        {
            var joint = new DistanceJoint(UidA, UidB, LocalAnchorA, LocalAnchorB)
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

            return joint;
        }

    }

    // 1-D rained system
    // m (v2 - v1) = lambda
    // v2 + (beta/h) * x1 + gamma * lambda = 0, gamma has units of inverse mass.
    // x2 = x1 + h * v2

    // 1-D mass-damper-spring system
    // m (v2 - v1) + h * d * v2 + h * k *

    // C = norm(p2 - p1) - L
    // u = (p2 - p1) / norm(p2 - p1)
    // Cdot = dot(u, v2 + cross(w2, r2) - v1 - cross(w1, r1))
    // J = [-u -cross(r1, u) u cross(r2, u)]
    // K = J * invM * JT
    //   = invMass1 + invI1 * cross(r1, u)^2 + invMass2 + invI2 * cross(r2, u)^2

    /// <summary>
    /// A distance joint rains two points on two bodies
    /// to remain at a fixed distance from each other. You can view
    /// this as a massless, rigid rod.
    /// </summary>
    public sealed class DistanceJoint : Joint, IEquatable<DistanceJoint>
    {
        // Sloth note:
        // Box2D is replacing rope with distance hence this is also a partial port of Box2D

         // Solver shared
        private float _bias;
        private float _gamma;
        private float _impulse;
        private float _lowerImpulse;
        private float _upperImpulse;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _u;
        private Vector2 _rA;
        private Vector2 _rB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private float _mass;
        private float _currentLength;
        private float _softMass;

        public override JointType JointType => JointType.Distance;

        /// <summary>
        /// This requires defining an
        /// anchor point on both bodies and the non-zero length of the
        /// distance joint. If you don't supply a length, the local anchor points
        /// is used so that the initial configuration can violate the constraint
        /// slightly. This helps when saving and loading a game.
        /// Warning Do not use a zero or short length.
        /// </summary>
        /// <param name="bodyA">The first body</param>
        /// <param name="bodyB">The second body</param>
        /// <param name="anchorA">The first body anchor</param>
        /// <param name="anchorB">The second body anchor</param>
        public DistanceJoint(EntityUid bodyA, EntityUid bodyB, Vector2 anchorA, Vector2 anchorB)
            : base(bodyA, bodyB)
        {
            // In case the bodies were swapped around
            if (bodyA != BodyAUid)
            {
                var anchor = anchorA;
                anchorA = anchorB;
                anchorB = anchor;
            }

            Length = MathF.Max(LinearSlop, (BodyB.GetWorldPoint(anchorB) - BodyA.GetWorldPoint(anchorA)).Length);
            _minLength = _length;
            _maxLength = _length;
            LocalAnchorA = anchorA;
            LocalAnchorB = anchorB;
        }

        /// <summary>
        /// The natural length between the anchor points.
        /// Manipulating the length can lead to non-physical behavior when the frequency is zero.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Length
        {
            get => _length;
            set
            {
                if (MathHelper.CloseTo(value, _length)) return;

                _impulse = 0.0f;
                _length = MathF.Max(value, LinearSlop);
                Dirty();
            }
        }

        private float _length;

        /// <summary>
        ///     The upper limit allowed between the 2 bodies.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxLength
        {
            get => _maxLength;
            set
            {
                if (MathHelper.CloseTo(value, _maxLength)) return;

                _upperImpulse = 0.0f;
                _maxLength = MathF.Max(value, _minLength);
                Dirty();
            }
        }

        private float _maxLength;

        /// <summary>
        ///     The lower limit allowed between the 2 bodies.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinLength
        {
            get => _minLength;
            set
            {
                if (MathHelper.CloseTo(value, _minLength)) return;

                _lowerImpulse = 0.0f;
                _minLength = Math.Clamp(value, LinearSlop, MaxLength);
                Dirty();
            }
        }

        private float _minLength;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Stiffness
        {
            get => _stiffness;
            set
            {
                if (MathHelper.CloseTo(_stiffness, value)) return;

                _stiffness = value;
                Dirty();
            }
        }

        private float _stiffness;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Damping
        {
            get => _damping;
            set
            {
                if (MathHelper.CloseTo(_damping, value)) return;

                _damping = value;
                Dirty();
            }
        }

        private float _damping;

        /// <summary>
        /// Get the reaction force given the inverse time step. Unit is N.
        /// </summary>
        /// <param name="invDt"></param>
        /// <returns></returns>
        public override Vector2 GetReactionForce(float invDt)
        {
            Vector2 F = _u * invDt * (_impulse + _lowerImpulse - _upperImpulse);
            return F;
        }

        public override JointState GetState()
        {
            var distanceState = new DistanceJointState
            {
                Damping = _damping,
                Length = _length,
                MinLength = _minLength,
                MaxLength = _maxLength,
                Stiffness = _stiffness,
                LocalAnchorA = LocalAnchorA,
                LocalAnchorB = LocalAnchorB
            };

            base.GetState(distanceState);
            return distanceState;
        }

        internal override void ApplyState(JointState state)
        {
            base.ApplyState(state);
            if (state is not DistanceJointState distanceState) return;

            _damping = distanceState.Damping;
            _length = distanceState.Length;
            _minLength = distanceState.MinLength;
            _maxLength = distanceState.MaxLength;
            _stiffness = distanceState.Stiffness;
        }

        /// <summary>
        /// Get the reaction torque given the inverse time step.
        /// Unit is N*m. This is always zero for a distance joint.
        /// </summary>
        /// <param name="invDt"></param>
        /// <returns></returns>
        public override float GetReactionTorque(float invDt)
        {
            return 0.0f;
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

	        var cA = data.Positions[_indexA];
	        float aA = data.Angles[_indexA];
	        var vA = data.LinearVelocities[_indexA];
	        float wA = data.AngularVelocities[_indexA];

	        var cB = data.Positions[_indexB];
	        float aB = data.Angles[_indexB];
	        var vB = data.LinearVelocities[_indexB];
	        float wB = data.AngularVelocities[_indexB];

	        Quaternion2D qA = new(aA), qB = new(aB);

	        _rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
	        _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
	        _u = cB + _rB - cA - _rA;

            // Handle singularity.
	        _currentLength = _u.Length;
	        if (_currentLength > LinearSlop)
	        {
		        _u *= 1.0f / _currentLength;
	        }
	        else
	        {
		        _u = Vector2.Zero;
		        _mass = 0.0f;
		        _impulse = 0.0f;
		        _lowerImpulse = 0.0f;
		        _upperImpulse = 0.0f;
	        }

	        float crAu = Vector2.Cross(_rA, _u);
	        float crBu = Vector2.Cross(_rB, _u);
	        float invMass = _invMassA + _invIA * crAu * crAu + _invMassB + _invIB * crBu * crBu;
	        _mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;

	        if (Stiffness > 0.0f && _minLength < _maxLength)
	        {
		        // soft
		        float C = _currentLength - _length;

		        float d = Damping;
		        float k = Stiffness;

		        // magic formulas
		        float h = data.FrameTime;

		        // gamma = 1 / (h * (d + h * k))
		        // the extra factor of h in the denominator is since the lambda is an impulse, not a force
		        _gamma = h * (d + h * k);
		        _gamma = _gamma != 0.0f ? 1.0f / _gamma : 0.0f;
		        _bias = C * h * k * _gamma;

		        invMass += _gamma;
		        _softMass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
	        }
	        else
	        {
		        // rigid
		        _gamma = 0.0f;
		        _bias = 0.0f;
		        _softMass = _mass;
	        }

	        if (data.WarmStarting)
	        {
		        // Scale the impulse to support a variable time step.
		        _impulse *= data.DtRatio;
		        _lowerImpulse *= data.DtRatio;
		        _upperImpulse *= data.DtRatio;

		        var P = _u * (_impulse + _lowerImpulse - _upperImpulse);
		        vA -= P * _invMassA;
		        wA -= _invIA * Vector2.Cross(_rA, P);
		        vB += P * _invMassB;
		        wB += _invIB * Vector2.Cross(_rB, P);
	        }
	        else
	        {
		        _impulse = 0.0f;
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

	        if (_minLength < _maxLength)
	        {
		        if (Stiffness > 0.0f)
		        {
			        // Cdot = dot(u, v + cross(w, r))
			        var vpA = vA + Vector2.Cross(wA, _rA);
			        var vpB = vB + Vector2.Cross(wB, _rB);
			        float Cdot = Vector2.Dot(_u, vpB - vpA);

			        float impulse = -_softMass * (Cdot + _bias + _gamma * _impulse);
			        _impulse += impulse;

                    // TODO: Ability to make this one-sided.
			        var P = _u * impulse;
			        vA -= P * _invMassA;
			        wA -= _invIA * Vector2.Cross(_rA, P);
			        vB += P * _invMassB;
			        wB += _invIB * Vector2.Cross(_rB, P);
		        }

		        // lower
		        {
			        float C = _currentLength - _minLength;
			        float bias = MathF.Max(0.0f, C) * data.InvDt;

			        var vpA = vA + Vector2.Cross(wA, _rA);
			        var vpB = vB + Vector2.Cross(wB, _rB);
			        float Cdot = Vector2.Dot(_u, vpB - vpA);

			        float impulse = -_mass * (Cdot + bias);
			        float oldImpulse = _lowerImpulse;
			        _lowerImpulse = MathF.Max(0.0f, _lowerImpulse + impulse);
			        impulse = _lowerImpulse - oldImpulse;
			        var P = _u * impulse;

			        vA -= P * _invMassA;
			        wA -= _invIA * Vector2.Cross(_rA, P);
			        vB += P * _invMassB;
			        wB += _invIB * Vector2.Cross(_rB, P);
		        }

		        // upper
		        {
			        float C = _maxLength - _currentLength;
			        float bias = MathF.Max(0.0f, C) * data.InvDt;

			        var vpA = vA + Vector2.Cross(wA, _rA);
			        var vpB = vB + Vector2.Cross(wB, _rB);
			        float Cdot = Vector2.Dot(_u, vpA - vpB);

			        float impulse = -_mass * (Cdot + bias);
			        float oldImpulse = _upperImpulse;
			        _upperImpulse = MathF.Max(0.0f, _upperImpulse + impulse);
			        impulse = _upperImpulse - oldImpulse;
			        var P = _u * -impulse;

			        vA -= P * _invMassA;
			        wA -= _invIA * Vector2.Cross(_rA, P);
			        vB += P * _invMassB;
			        wB += _invIB * Vector2.Cross(_rB, P);
		        }
	        }
	        else
	        {
		        // Equal limits

		        // Cdot = dot(u, v + cross(w, r))
		        var vpA = vA + Vector2.Cross(wA, _rA);
		        var vpB = vB + Vector2.Cross(wB, _rB);
		        float Cdot = Vector2.Dot(_u, vpB - vpA);

		        float impulse = -_mass * Cdot;
		        _impulse += impulse;

		        var P = _u * impulse;
		        vA -= P * _invMassA;
		        wA -= _invIA * Vector2.Cross(_rA, P);
		        vB += P * _invMassB;
		        wB += _invIB * Vector2.Cross(_rB, P);
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

            var rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
            var rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
            var u = cB + rB - cA - rA;

            float length = u.Length;
            u = u.Normalized;
            float C;
            if (MathHelper.CloseTo(_minLength, _maxLength))
            {
                C = length - _minLength;
            }
            else if (length < _minLength)
            {
                C = length - _minLength;
            }
            else if (_maxLength < length)
            {
                C = length - _maxLength;
            }
            else
            {
                return true;
            }

            float impulse = -_mass * C;
            var P = u * impulse;

            cA -= P * _invMassA;
            aA -= _invIA * Vector2.Cross(rA, P);
            cB += P * _invMassB;
            aB += _invIB * Vector2.Cross(rB, P);

            data.Positions[_indexA] = cA;
            data.Angles[_indexA] = aA;
            data.Positions[_indexB] = cB;
            data.Angles[_indexB] = aB;

            return MathF.Abs(C) < data.LinearSlop;
        }

        public bool Equals(DistanceJoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!base.Equals(other)) return false;

            return MathHelper.CloseTo(_length, other._length) &&
                   MathHelper.CloseTo(_minLength, other._minLength) &&
                   MathHelper.CloseTo(_maxLength, other._maxLength) &&
                   MathHelper.CloseTo(_stiffness, other._stiffness) &&
                   MathHelper.CloseTo(_damping, other._damping);
        }
    }
}
