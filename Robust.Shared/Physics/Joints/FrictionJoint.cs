using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    /// <summary>
    /// Friction joint. This is used for top-down friction.
    /// It provides 2D translational friction and angular friction.
    /// </summary>
    public sealed class FrictionJoint: Joint
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
        private Mat22 _linearMass;

        internal FrictionJoint()
        {
            JointType = JointType.Friction;
        }

        /// <summary>
        /// Constructor for FrictionJoint.
        /// </summary>
        /// <param name="bodyA"></param>
        /// <param name="bodyB"></param>
        /// <param name="anchor"></param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public FrictionJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchor, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Friction;

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

        /// <summary>
        /// The local anchor point on BodyA
        /// </summary>
        public Vector2 LocalAnchorA { get; set; }

        /// <summary>
        /// The local anchor point on BodyB
        /// </summary>
        public Vector2 LocalAnchorB { get; set; }

        public override Vector2 WorldAnchorA
        {
            get { return BodyA.GetWorldPoint(LocalAnchorA); }
            set { LocalAnchorA = BodyA.GetLocalPoint(value); }
        }

        public override Vector2 WorldAnchorB
        {
            get { return BodyB.GetWorldPoint(LocalAnchorB); }
            set { LocalAnchorB = BodyB.GetLocalPoint(value); }
        }

        /// <summary>
        /// The maximum friction force in N.
        /// </summary>
        public float MaxForce { get; set; }

        /// <summary>
        /// The maximum friction torque in N-m.
        /// </summary>
        public float MaxTorque { get; set; }

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

            float aA = data.Positions[_indexA].Angle;
            Vector2 vA = data.Velocities[_indexA].LinearVelocity;
            float wA = data.Velocities[_indexA].AngularVelocity;

            float aB = data.Positions[_indexB].Angle;
            Vector2 vB = data.Velocities[_indexB].LinearVelocity;
            float wB = data.Velocities[_indexB].AngularVelocity;

            Complex qA = Complex.FromAngle(aA);
            Complex qB = Complex.FromAngle(aB);

            // Compute the effective mass matrix.
            _rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            _rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);

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

                Vector2 impulse = -PhysicsMath.Mul(ref _linearMass, ref Cdot);
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
