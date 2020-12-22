using System;
using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    /// <summary>
    /// A motor joint is used to control the relative motion
    /// between two bodies. A typical usage is to control the movement
    /// of a dynamic body with respect to the ground.
    /// </summary>
    public sealed class MotorJoint : Joint
    {
        // Solver shared
        private Vector2 _linearOffset;
        private float _angularOffset;
        private Vector2 _linearImpulse;
        private float _angularImpulse;
        private float _maxForce;
        private float _maxTorque;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _rA;
        private Vector2 _rB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private Vector2 _linearError;
        private float _angularError;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private Mat22 _linearMass;
        private float _angularMass;

        internal MotorJoint()
        {
            JointType = JointType.Motor;
        }

        /// <summary>
        /// Constructor for MotorJoint.
        /// </summary>
        /// <param name="bodyA">The first body</param>
        /// <param name="bodyB">The second body</param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public MotorJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Motor;

            Vector2 xB = BodyB.Owner.Transform.WorldPosition;

            if (useWorldCoordinates)
                _linearOffset = BodyA.GetLocalPoint(xB);
            else
                _linearOffset = xB;

            //Defaults
            _angularOffset = 0.0f;
            _maxForce = 1.0f;
            _maxTorque = 1.0f;
            CorrectionFactor = 0.3f;

            _angularOffset = BodyB.Rotation - BodyA.Rotation;
        }

        public override Vector2 WorldAnchorA
        {
            get { return BodyA.Owner.Transform.WorldPosition; }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        public override Vector2 WorldAnchorB
        {
            get { return BodyB.Owner.Transform.WorldPosition; }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        /// <summary>
        /// The maximum amount of force that can be applied to BodyA
        /// </summary>
        public float MaxForce
        {
            get { return _maxForce; }
            set
            {
                Debug.Assert(!float.IsNaN(value) && value >= 0.0f);
                _maxForce = value;
            }
        }

        /// <summary>
        /// The maximum amount of torque that can be applied to BodyA
        /// </summary>
        public float MaxTorque
        {
            get { return _maxTorque; }
            set
            {
                Debug.Assert(!float.IsNaN(value) && value >= 0.0f);
                _maxTorque = value;
            }
        }

        /// <summary>
        /// The linear (translation) offset.
        /// </summary>
        public Vector2 LinearOffset
        {
            get { return _linearOffset; }
            set
            {
                if (_linearOffset.X != value.X || _linearOffset.Y != value.Y)
                {
                    WakeBodies();
                    _linearOffset = value;
                }
            }
        }

        /// <summary>
        /// Get or set the angular offset.
        /// </summary>
        public float AngularOffset
        {
            set
            {
                if (_angularOffset != value)
                {
                    WakeBodies();
                    _angularOffset = value;
                }
            }
            get { return _angularOffset; }
        }

        //FPE note: Used for serialization.
        internal float CorrectionFactor { get; set; }

        public override Vector2 GetReactionForce(float invDt)
        {
            return _linearImpulse * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _angularImpulse;
        }

        internal override void InitVelocityConstraints(ref SolverData data)
        {
            _indexA = BodyA.IslandIndex;
            _indexB = BodyB.IslandIndex;
            _localCenterA = BodyA.Sweep.LocalCenter;
            _localCenterB = BodyB.Sweep.LocalCenter;
            _invMassA = BodyA.InvMass;
            _invMassB = BodyB.InvMass;
            _invIA = BodyA.InvI;
            _invIB = BodyB.InvI;

            Vector2 cA = data.Positions[_indexA].Center;
            float aA = data.Positions[_indexA].Angle;
            Vector2 vA = data.Velocities[_indexA].LinearVelocity;
            float wA = data.Velocities[_indexA].AngularVelocity;

            Vector2 cB = data.Positions[_indexB].Center;
            float aB = data.Positions[_indexB].Angle;
            Vector2 vB = data.Velocities[_indexB].LinearVelocity;
            float wB = data.Velocities[_indexB].AngularVelocity;

            Complex qA = Complex.FromAngle(aA);
            Complex qB = Complex.FromAngle(aB);

            // Compute the effective mass matrix.
            _rA = -Complex.Multiply(_localCenterA, ref qA);
            _rB = -Complex.Multiply(_localCenterB, ref qB);

            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
            //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
            //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            Mat22 K = new Mat22();
            K.ex.X = mA + mB + iA * _rA.Y * _rA.Y + iB * _rB.Y * _rB.Y;
            K.ex.Y = -iA * _rA.X * _rA.Y - iB * _rB.X * _rB.Y;
            K.ey.X = K.ex.Y;
            K.ey.Y = mA + mB + iA * _rA.X * _rA.X + iB * _rB.X * _rB.X;

            _linearMass = K.Inverse;

            _angularMass = iA + iB;
            if (_angularMass > 0.0f)
            {
                _angularMass = 1.0f / _angularMass;
            }

            _linearError = cB + _rB - cA - _rA - Complex.Multiply(_linearOffset, ref qA);
            _angularError = aB - aA - _angularOffset;

            if (data.Step.WarmStarting)
            {
                // Scale impulses to support a variable time step.
                _linearImpulse *= data.Step.DtRatio;
                _angularImpulse *= data.Step.DtRatio;

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

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;
        }

        internal override void SolveVelocityConstraints(ref SolverData data)
        {
            Vector2 vA = data.Velocities[_indexA].LinearVelocity;
            float wA = data.Velocities[_indexA].AngularVelocity;
            Vector2 vB = data.Velocities[_indexB].LinearVelocity;
            float wB = data.Velocities[_indexB].AngularVelocity;

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            float h = data.Step.DeltaTime;
            float inv_h = data.Step.InvDt;

            // Solve angular friction
            {
                float Cdot = wB - wA + inv_h * CorrectionFactor * _angularError;
                float impulse = -_angularMass * Cdot;

                float oldImpulse = _angularImpulse;
                float maxImpulse = h * _maxTorque;
                _angularImpulse = Math.Clamp(_angularImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _angularImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve linear friction
            {
                Vector2 Cdot = vB + Vector2.Cross(wB, _rB) - vA - Vector2.Cross(wA, _rA) + _linearError * CorrectionFactor * inv_h;

                Vector2 impulse = -PhysicsMath.Mul(ref _linearMass, ref Cdot);
                Vector2 oldImpulse = _linearImpulse;
                _linearImpulse += impulse;

                float maxImpulse = h * _maxForce;

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

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;
        }

        internal override bool SolvePositionConstraints(ref SolverData data)
        {
            return true;
        }
    }
}
