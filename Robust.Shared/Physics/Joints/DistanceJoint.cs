using System;
using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    public sealed class DistanceJoint : Joint
    {
        // Solver shared
        private float _bias;
        private float _gamma;
        private float _impulse;

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

        internal DistanceJoint()
        {
            JointType = JointType.Distance;
        }

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
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public DistanceJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchorA, Vector2 anchorB, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Distance;

            if (useWorldCoordinates)
            {
                LocalAnchorA = bodyA.GetLocalPoint(anchorA);
                LocalAnchorB = bodyB.GetLocalPoint(anchorB);
                Length = (anchorB - anchorA).Length;
            }
            else
            {
                LocalAnchorA = anchorA;
                LocalAnchorB = anchorB;
                Length = (BodyB.GetWorldPoint(anchorB) - BodyA.GetWorldPoint(anchorA)).Length;
            }
        }

        /// <summary>
        /// The local anchor point relative to bodyA's origin.
        /// </summary>
        public Vector2 LocalAnchorA { get; set; }

        /// <summary>
        /// The local anchor point relative to bodyB's origin.
        /// </summary>
        public Vector2 LocalAnchorB { get; set; }

        public override sealed Vector2 WorldAnchorA
        {
            get { return BodyA.GetWorldPoint(LocalAnchorA); }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        public override sealed Vector2 WorldAnchorB
        {
            get { return BodyB.GetWorldPoint(LocalAnchorB); }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        /// <summary>
        /// The natural length between the anchor points.
        /// Manipulating the length can lead to non-physical behavior when the frequency is zero.
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// The mass-spring-damper frequency in Hertz. A value of 0
        /// disables softness.
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// The damping ratio. 0 = no damping, 1 = critical damping.
        /// </summary>
        public float DampingRatio { get; set; }

        /// <summary>
        /// Get the reaction force given the inverse time step. Unit is N.
        /// </summary>
        /// <param name="invDt"></param>
        /// <returns></returns>
        public override Vector2 GetReactionForce(float invDt)
        {
            Vector2 F = _u * (invDt * _impulse);
            return F;
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

            _rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            _rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);
            _u = cB + _rB - cA - _rA;

            // Handle singularity.
            float length = _u.Length;
            if (length > PhysicsSettings.LinearSlop)
            {
                _u *= 1.0f / length;
            }
            else
            {
                _u = Vector2.Zero;
            }

            float crAu = Vector2.Cross(_rA, _u);
            float crBu = Vector2.Cross(_rB, _u);
            float invMass = _invMassA + _invIA * crAu * crAu + _invMassB + _invIB * crBu * crBu;

            // Compute the effective mass matrix.
            _mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;

            if (Frequency > 0.0f)
            {
                float C = length - Length;

                // Frequency
                // TODO: Tau
                float omega = 2f * MathF.PI * Frequency;

                // Damping coefficient
                float d = 2.0f * _mass * DampingRatio * omega;

                // Spring stiffness
                float k = _mass * omega * omega;

                // magic formulas
                float h = data.Step.DeltaTime;
                _gamma = h * (d + h * k);
                _gamma = _gamma != 0.0f ? 1.0f / _gamma : 0.0f;
                _bias = C * h * k * _gamma;

                invMass += _gamma;
                _mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
            }
            else
            {
                _gamma = 0.0f;
                _bias = 0.0f;
            }

            if (data.Step.WarmStarting)
            {
                // Scale the impulse to support a variable time step.
                _impulse *= data.Step.DtRatio;

                Vector2 P = _u * _impulse;
                vA -= P * _invMassA;
                wA -= _invIA * Vector2.Cross(_rA, P);
                vB += P * _invMassB;
                wB += _invIB * Vector2.Cross(_rB, P);
            }
            else
            {
                _impulse = 0.0f;
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

            // Cdot = dot(u, v + cross(w, r))
            Vector2 vpA = vA + Vector2.Cross(wA, _rA);
            Vector2 vpB = vB + Vector2.Cross(wB, _rB);
            float Cdot = Vector2.Dot(_u, vpB - vpA);

            float impulse = -_mass * (Cdot + _bias + _gamma * _impulse);
            _impulse += impulse;

            Vector2 P = _u * impulse;
            vA -= P * _invMassA;
            wA -= _invIA * Vector2.Cross(_rA, P);
            vB += P * _invMassB;
            wB += _invIB * Vector2.Cross(_rB, P);

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;

        }

        internal override bool SolvePositionConstraints(ref SolverData data)
        {
            if (Frequency > 0.0f)
            {
                // There is no position correction for soft distance constraints.
                return true;
            }

            Vector2 cA = data.Positions[_indexA].Center;
            float aA = data.Positions[_indexA].Angle;
            Vector2 cB = data.Positions[_indexB].Center;
            float aB = data.Positions[_indexB].Angle;

            Complex qA = Complex.FromAngle(aA);
            Complex qB = Complex.FromAngle(aB);

            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);
            Vector2 u = cB + rB - cA - rA;

            float length = u.Length;
            u = u.Normalized;
            float C = length - Length;
            C = Math.Clamp(C, -PhysicsSettings.MaxLinearCorrection, PhysicsSettings.MaxLinearCorrection);

            float impulse = -_mass * C;
            Vector2 P = u * impulse;

            cA -= P * _invMassA;
            aA -= _invIA * Vector2.Cross(rA, P);
            cB += P * _invMassB;
            aB += _invIB * Vector2.Cross(rB, P);

            data.Positions[_indexA].Center = cA;
            data.Positions[_indexA].Angle = aA;
            data.Positions[_indexB].Center = cB;
            data.Positions[_indexB].Angle = aB;

            return Math.Abs(C) < PhysicsSettings.LinearSlop;
        }
    }
}
