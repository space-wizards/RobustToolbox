using System;
using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    // Pulley:
    // length1 = norm(p1 - s1)
    // length2 = norm(p2 - s2)
    // C0 = (length1 + ratio * length2)_initial
    // C = C0 - (length1 + ratio * length2)
    // u1 = (p1 - s1) / norm(p1 - s1)
    // u2 = (p2 - s2) / norm(p2 - s2)
    // Cdot = -dot(u1, v1 + cross(w1, r1)) - ratio * dot(u2, v2 + cross(w2, r2))
    // J = -[u1 cross(r1, u1) ratio * u2  ratio * cross(r2, u2)]
    // K = J * invM * JT
    //   = invMass1 + invI1 * cross(r1, u1)^2 + ratio^2 * (invMass2 + invI2 * cross(r2, u2)^2)

    /// <summary>
    /// The pulley joint is connected to two bodies and two fixed world points.
    /// The pulley supports a ratio such that:
    /// <![CDATA[length1 + ratio * length2 <= constant]]>
    /// Yes, the force transmitted is scaled by the ratio.
    ///
    /// Warning: the pulley joint can get a bit squirrelly by itself. They often
    /// work better when combined with prismatic joints. You should also cover the
    /// the anchor points with static shapes to prevent one side from going to zero length.
    /// </summary>
    public sealed class PulleyJoint : Joint
    {
        // Solver shared
        private float _impulse;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _uA;
        private Vector2 _uB;
        private Vector2 _rA;
        private Vector2 _rB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private float _mass;

        internal PulleyJoint()
        {
            JointType = JointType.Pulley;
        }

        /// <summary>
        /// Constructor for PulleyJoint.
        /// </summary>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchorA">The anchor on the first body.</param>
        /// <param name="anchorB">The anchor on the second body.</param>
        /// <param name="worldAnchorA">The world anchor for the first body.</param>
        /// <param name="worldAnchorB">The world anchor for the second body.</param>
        /// <param name="ratio">The ratio.</param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public PulleyJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchorA, Vector2 anchorB, Vector2 worldAnchorA, Vector2 worldAnchorB, float ratio, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Pulley;

            WorldAnchorA = worldAnchorA;
            WorldAnchorB = worldAnchorB;

            if (useWorldCoordinates)
            {
                LocalAnchorA = BodyA.GetLocalPoint(anchorA);
                LocalAnchorB = BodyB.GetLocalPoint(anchorB);

                Vector2 dA = anchorA - worldAnchorA;
                LengthA = dA.Length;
                Vector2 dB = anchorB - worldAnchorB;
                LengthB = dB.Length;
            }
            else
            {
                LocalAnchorA = anchorA;
                LocalAnchorB = anchorB;

                Vector2 dA = anchorA - BodyA.GetLocalPoint(worldAnchorA);
                LengthA = dA.Length;
                Vector2 dB = anchorB - BodyB.GetLocalPoint(worldAnchorB);
                LengthB = dB.Length;
            }

            Debug.Assert(ratio != 0.0f);
            Debug.Assert(ratio > float.Epsilon);

            Ratio = ratio;
            Constant = LengthA + ratio * LengthB;
            _impulse = 0.0f;
        }

        /// <summary>
        /// The local anchor point on BodyA
        /// </summary>
        public Vector2 LocalAnchorA { get; set; }

        /// <summary>
        /// The local anchor point on BodyB
        /// </summary>
        public Vector2 LocalAnchorB { get; set; }

        /// <summary>
        /// Get the first world anchor.
        /// </summary>
        /// <value></value>
        public override sealed Vector2 WorldAnchorA { get; set; }

        /// <summary>
        /// Get the second world anchor.
        /// </summary>
        /// <value></value>
        public override sealed Vector2 WorldAnchorB { get; set; }

        /// <summary>
        /// Get the current length of the segment attached to body1.
        /// </summary>
        /// <value></value>
        public float LengthA { get; set; }

        /// <summary>
        /// Get the current length of the segment attached to body2.
        /// </summary>
        /// <value></value>
        public float LengthB { get; set; }

        /// <summary>
        /// The current length between the anchor point on BodyA and WorldAnchorA
        /// </summary>
        public float CurrentLengthA
        {
            get
            {
                Vector2 p = BodyA.GetWorldPoint(LocalAnchorA);
                Vector2 s = WorldAnchorA;
                Vector2 d = p - s;
                return d.Length;
            }
        }

        /// <summary>
        /// The current length between the anchor point on BodyB and WorldAnchorB
        /// </summary>
        public float CurrentLengthB
        {
            get
            {
                Vector2 p = BodyB.GetWorldPoint(LocalAnchorB);
                Vector2 s = WorldAnchorB;
                Vector2 d = p - s;
                return d.Length;
            }
        }

        /// <summary>
        /// Get the pulley ratio.
        /// </summary>
        /// <value></value>
        public float Ratio { get; set; }

        //FPE note: Only used for serialization.
        internal float Constant { get; set; }

        public override Vector2 GetReactionForce(float invDt)
        {
            Vector2 P = _uB * _impulse;
            return P * invDt;
        }

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

            // Get the pulley axes.
            _uA = cA + _rA - WorldAnchorA;
            _uB = cB + _rB - WorldAnchorB;

            float lengthA = _uA.Length;
            float lengthB = _uB.Length;

            if (lengthA > 10.0f * PhysicsSettings.LinearSlop)
            {
                _uA *= 1.0f / lengthA;
            }
            else
            {
                _uA = Vector2.Zero;
            }

            if (lengthB > 10.0f * PhysicsSettings.LinearSlop)
            {
                _uB *= 1.0f / lengthB;
            }
            else
            {
                _uB = Vector2.Zero;
            }

            // Compute effective mass.
            float ruA = Vector2.Cross(_rA, _uA);
            float ruB = Vector2.Cross(_rB, _uB);

            float mA = _invMassA + _invIA * ruA * ruA;
            float mB = _invMassB + _invIB * ruB * ruB;

            _mass = mA + Ratio * Ratio * mB;

            if (_mass > 0.0f)
            {
                _mass = 1.0f / _mass;
            }

            if (data.Step.WarmStarting)
            {
                // Scale impulses to support variable time steps.
                _impulse *= data.Step.DtRatio;

                // Warm starting.
                Vector2 PA = _uA * -(_impulse);
                Vector2 PB = _uB * (-Ratio * _impulse);

                vA += PA * _invMassA;
                wA += _invIA * Vector2.Cross(_rA, PA);
                vB += PB * _invMassB;
                wB += _invIB * Vector2.Cross(_rB, PB);
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

            Vector2 vpA = vA + Vector2.Cross(wA, _rA);
            Vector2 vpB = vB + Vector2.Cross(wB, _rB);

            float Cdot = -Vector2.Dot(_uA, vpA) - Ratio * Vector2.Dot(_uB, vpB);
            float impulse = -_mass * Cdot;
            _impulse += impulse;

            Vector2 PA = _uA * -impulse;
            Vector2 PB = _uB * -Ratio * impulse;
            vA += PA * _invMassA;
            wA += _invIA * Vector2.Cross(_rA, PA);
            vB += PB * _invMassB;
            wB += _invIB * Vector2.Cross(_rB, PB);

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;
        }

        internal override bool SolvePositionConstraints(ref SolverData data)
        {
            Vector2 cA = data.Positions[_indexA].Center;
            float aA = data.Positions[_indexA].Angle;
            Vector2 cB = data.Positions[_indexB].Center;
            float aB = data.Positions[_indexB].Angle;

            Complex qA = Complex.FromAngle(aA);
            Complex qB = Complex.FromAngle(aB);

            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);

            // Get the pulley axes.
            Vector2 uA = cA + rA - WorldAnchorA;
            Vector2 uB = cB + rB - WorldAnchorB;

            float lengthA = uA.Length;
            float lengthB = uB.Length;

            if (lengthA > 10.0f * PhysicsSettings.LinearSlop)
            {
                uA *= 1.0f / lengthA;
            }
            else
            {
                uA = Vector2.Zero;
            }

            if (lengthB > 10.0f * PhysicsSettings.LinearSlop)
            {
                uB *= 1.0f / lengthB;
            }
            else
            {
                uB = Vector2.Zero;
            }

            // Compute effective mass.
            float ruA = Vector2.Cross(rA, uA);
            float ruB = Vector2.Cross(rB, uB);

            float mA = _invMassA + _invIA * ruA * ruA;
            float mB = _invMassB + _invIB * ruB * ruB;

            float mass = mA + Ratio * Ratio * mB;

            if (mass > 0.0f)
            {
                mass = 1.0f / mass;
            }

            float C = Constant - lengthA - Ratio * lengthB;
            float linearError = Math.Abs(C);

            float impulse = -mass * C;

            Vector2 PA = uA * -impulse;
            Vector2 PB = uB * -Ratio * impulse;

            cA += PA * _invMassA;
            aA += _invIA * Vector2.Cross(rA, PA);
            cB += PB * _invMassB;
            aB += _invIB * Vector2.Cross(rB, PB);

            data.Positions[_indexA].Center = cA;
            data.Positions[_indexA].Angle = aA;
            data.Positions[_indexB].Center = cB;
            data.Positions[_indexB].Angle = aB;

            return linearError < PhysicsSettings.LinearSlop;
        }
    }
}
