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
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
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
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class DistanceJoint : Joint, IEquatable<DistanceJoint>
    {
        // Sloth note:
        // Box2D is replacing rope with distance hence this is also a partial port of Box2D

         // Solver shared
        [NonSerialized] private float _bias;
        [NonSerialized] private float _gamma;
        [NonSerialized] private float _impulse;
        [NonSerialized] private float _lowerImpulse;
        [NonSerialized] private float _upperImpulse;

        // Solver temp
        [NonSerialized] private int _indexA;
        [NonSerialized] private int _indexB;
        [NonSerialized] private Vector2 _u;
        [NonSerialized] private Vector2 _rA;
        [NonSerialized] private Vector2 _rB;
        [NonSerialized] private Vector2 _localCenterA;
        [NonSerialized] private Vector2 _localCenterB;
        [NonSerialized] private float _invMassA;
        [NonSerialized] private float _invMassB;
        [NonSerialized] private float _invIA;
        [NonSerialized] private float _invIB;
        [NonSerialized] private float _mass;
        [NonSerialized] private float _currentLength;
        [NonSerialized] private float _softMass;

        [NonSerialized] private float _linearSlop;

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
        public DistanceJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchorA, Vector2 anchorB)
            : base(bodyA, bodyB)
        {
            LocalAnchorA = anchorA;
            LocalAnchorB = anchorB;
            // TODO: Just pass this into the ctor.
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            Length = MathF.Max(configManager.GetCVar(CVars.LinearSlop), (BodyB.GetWorldPoint(anchorB) - BodyA.GetWorldPoint(anchorA)).Length);
            WarmStarting = configManager.GetCVar(CVars.WarmStarting);
            _linearSlop = configManager.GetCVar(CVars.LinearSlop);
            _minLength = _length;
            _maxLength = _length;
        }

        /// <summary>
        ///     Does the DistanceJoint warmstart? Can be overridden from the cvar default.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool WarmStarting
        {
            get => _warmStarting;
            set
            {
                if (_warmStarting == value) return;

                _warmStarting = value;
                Dirty();
            }
        }

        private bool _warmStarting;

        /// <summary>
        /// The local anchor point relative to bodyA's origin.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalAnchorA
        {
            get => _localAnchorA;
            set
            {
                if (_localAnchorA.EqualsApprox(value)) return;

                _localAnchorA = value;
                Dirty();
            }
        }

        private Vector2 _localAnchorA;

        /// <summary>
        /// The local anchor point relative to bodyB's origin.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalAnchorB
        {
            get => _localAnchorB;
            set
            {
                if (_localAnchorB.EqualsApprox(value)) return;

                _localAnchorB = value;
                Dirty();
            }
        }

        private Vector2 _localAnchorB;

        public override Vector2 WorldAnchorA
        {
            get => BodyA.GetWorldPoint(LocalAnchorA);
            set => DebugTools.Assert(false, "You can't set the world anchor on this joint type.");
        }

        public override Vector2 WorldAnchorB
        {
            get => BodyB.GetWorldPoint(LocalAnchorB);
            set => DebugTools.Assert(false, "You can't set the world anchor on this joint type.");
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
                _length = MathF.Max(IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.LinearSlop), _length);
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
                _minLength = Math.Clamp(_minLength, IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.LinearSlop), MaxLength);
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

        public void LinearStiffness(float frequency, float dampingRatio)
        {
            var massA = BodyA.Mass;
            var massB = BodyB.Mass;
            float mass;

            if (massA > 0.0f && massB > 0.0f)
            {
                mass = massA * massB / (massA + massB);
            }
            else if (massA > 0.0f)
            {
                mass = massA;
            }
            else
            {
                mass = massB;
            }

            var omega = 2.0f * MathF.PI * frequency;
            Stiffness = mass * omega * omega;
            Damping = 2.0f * mass * dampingRatio * omega;
        }

        public override void DebugDraw(DebugDrawingHandle handle, in Box2 worldViewport)
        {
            base.DebugDraw(handle, in worldViewport);

            var matrixB = BodyB.Owner.Transform.WorldMatrix;
            var vector = WorldAnchorA - WorldAnchorB;
            var distance = vector.Length;

            if (distance <= 0.0f) return;

            var line = new Box2Rotated(new Box2(-0.08f, 0, 0.08f, distance), vector.ToWorldAngle().Opposite());

            handle.SetTransform(matrixB);
            handle.DrawRect(line, Color.Blue.WithAlpha(0.7f));
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

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var linearSlop = configManager.GetCVar(CVars.LinearSlop);

	        // Handle singularity.
	        _currentLength = _u.Length;
	        if (_currentLength > linearSlop)
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

	        if (_warmStarting)
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

            return MathF.Abs(C) < _linearSlop;
        }

        public bool Equals(DistanceJoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return LocalAnchorA.EqualsApprox(other.LocalAnchorA) &&
                   LocalAnchorB.EqualsApprox(other.LocalAnchorB) &&
                   MathHelper.CloseTo(Length, other.Length) &&
                   MathHelper.CloseTo(Stiffness, other.Stiffness) &&
                   MathHelper.CloseTo(Damping, other.Damping) &&
                   MathHelper.CloseTo(MaxLength, other.MaxLength) &&
                   MathHelper.CloseTo(MinLength, other.MinLength);
        }
    }
}
