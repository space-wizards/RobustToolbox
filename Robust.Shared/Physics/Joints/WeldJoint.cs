using System;
using System.Reflection.Metadata;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    // Point-to-point constraint
    // C = p2 - p1
    // Cdot = v2 - v1
    //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
    // J = [-I -r1_skew I r2_skew ]
    // Identity used:
    // w k % (rx i + ry j) = w * (-ry i + rx j)

    // Angle constraint
    // C = angle2 - angle1 - referenceAngle
    // Cdot = w2 - w1
    // J = [0 0 -1 0 0 1]
    // K = invI1 + invI2

    /// <summary>
    /// A weld joint essentially glues two bodies together. A weld joint may
    /// distort somewhat because the island constraint solver is approximate.
    ///
    /// The joint is soft constraint based, which means the two bodies will move
    /// relative to each other, when a force is applied. To combine two bodies
    /// in a rigid fashion, combine the fixtures to a single body instead.
    /// </summary>
    public class WeldJoint : Joint
    {
        // Solver shared
        private Vector3 _impulse;
        private float _gamma;
        private float _bias;

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
        private Mat33 _mass;

        internal WeldJoint()
        {
            JointType = JointType.Weld;
        }

        /// <summary>
        /// You need to specify an anchor point where they are attached.
        /// The position of the anchor point is important for computing the reaction torque.
        /// </summary>
        /// <param name="bodyA">The first body</param>
        /// <param name="bodyB">The second body</param>
        /// <param name="anchorA">The first body anchor.</param>
        /// <param name="anchorB">The second body anchor.</param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public WeldJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchorA, Vector2 anchorB, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Weld;

            if (useWorldCoordinates)
            {
                LocalAnchorA = bodyA.GetLocalPoint(anchorA);
                LocalAnchorB = bodyB.GetLocalPoint(anchorB);
            }
            else
            {
                LocalAnchorA = anchorA;
                LocalAnchorB = anchorB;
            }

            ReferenceAngle = BodyB.Rotation - BodyA.Rotation;
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
        /// The bodyB angle minus bodyA angle in the reference state (radians).
        /// </summary>
        public float ReferenceAngle { get; set; }

        /// <summary>
        /// The frequency of the joint. A higher frequency means a stiffer joint, but
        /// a too high value can cause the joint to oscillate.
        /// Default is 0, which means the joint does no spring calculations.
        /// </summary>
        public float FrequencyHz { get; set; }

        /// <summary>
        /// The damping on the joint. The damping is only used when
        /// the joint has a frequency (> 0). A higher value means more damping.
        /// </summary>
        public float DampingRatio { get; set; }

        public override Vector2 GetReactionForce(float invDt)
        {
            return new Vector2(_impulse.X, _impulse.Y) * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _impulse.Z;
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

            Mat33 K = new Mat33();
            K.ex.X = mA + mB + _rA.Y * _rA.Y * iA + _rB.Y * _rB.Y * iB;
            K.ey.X = -_rA.Y * _rA.X * iA - _rB.Y * _rB.X * iB;
            K.ez.X = -_rA.Y * iA - _rB.Y * iB;
            K.ex.Y = K.ey.X;
            K.ey.Y = mA + mB + _rA.X * _rA.X * iA + _rB.X * _rB.X * iB;
            K.ez.Y = _rA.X * iA + _rB.X * iB;
            K.ex.Z = K.ez.X;
            K.ey.Z = K.ez.Y;
            K.ez.Z = iA + iB;

            if (FrequencyHz > 0.0f)
            {
                K.GetInverse22(ref _mass);

                float invM = iA + iB;
                float m = invM > 0.0f ? 1.0f / invM : 0.0f;

                float C = aB - aA - ReferenceAngle;

                // Frequency
                // TODO: Tau
                float omega = 2 * MathF.PI * FrequencyHz;

                // Damping coefficient
                float d = 2.0f * m * DampingRatio * omega;

                // Spring stiffness
                float k = m * omega * omega;

                // magic formulas
                float h = data.Step.DeltaTime;
                _gamma = h * (d + h * k);
                _gamma = _gamma != 0.0f ? 1.0f / _gamma : 0.0f;
                _bias = C * h * k * _gamma;

                invM += _gamma;
                _mass.ez.Z = invM != 0.0f ? 1.0f / invM : 0.0f;
            }
            else if (K.ez.Z == 0.0f)
            {
                K.GetInverse22(ref _mass);
                _gamma = 0.0f;
                _bias = 0.0f;
            }
            else
            {
                K.GetSymInverse33(ref _mass);
                _gamma = 0.0f;
                _bias = 0.0f;
            }

            if (data.Step.WarmStarting)
            {
                // Scale impulses to support a variable time step.
                _impulse *= data.Step.DtRatio;

                Vector2 P = new Vector2(_impulse.X, _impulse.Y);

                vA -= P * mA;
                wA -= iA * (Vector2.Cross( _rA, P) + _impulse.Z);

                vB += P * mB;
                wB += iB * (Vector2.Cross( _rB, P) + _impulse.Z);
            }
            else
            {
                _impulse = Vector3.Zero;
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

            if (FrequencyHz > 0.0f)
            {
                float Cdot2 = wB - wA;

                float impulse2 = -_mass.ez.Z * (Cdot2 + _bias + _gamma * _impulse.Z);
                _impulse.Z += impulse2;

                wA -= iA * impulse2;
                wB += iB * impulse2;

                Vector2 Cdot1 = vB + Vector2.Cross(wB, _rB) - vA - Vector2.Cross(wA, _rA);

                Vector2 impulse1 = -PhysicsMath.Mul22(_mass, Cdot1);
                _impulse.X += impulse1.X;
                _impulse.Y += impulse1.Y;

                Vector2 P = impulse1;

                vA -= P * mA;
                wA -= iA * Vector2.Cross( _rA, P);

                vB += P * mB;
                wB += iB * Vector2.Cross( _rB, P);
            }
            else
            {
                Vector2 Cdot1 = vB + Vector2.Cross(wB, _rB) - vA - Vector2.Cross(wA, _rA);
                float Cdot2 = wB - wA;
                Vector3 Cdot = new Vector3(Cdot1.X, Cdot1.Y, Cdot2);

                Vector3 impulse = -PhysicsMath.Mul(_mass, Cdot);
                _impulse += impulse;

                Vector2 P = new Vector2(impulse.X, impulse.Y);

                vA -= P * mA;
                wA -= iA * (Vector2.Cross( _rA, P) + impulse.Z);

                vB += P * mB;
                wB += iB * (Vector2.Cross( _rB, P) + impulse.Z);
            }

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

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);

            float positionError, angularError;

            Mat33 K = new Mat33();
            K.ex.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
            K.ey.X = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
            K.ez.X = -rA.Y * iA - rB.Y * iB;
            K.ex.Y = K.ey.X;
            K.ey.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
            K.ez.Y = rA.X * iA + rB.X * iB;
            K.ex.Z = K.ez.X;
            K.ey.Z = K.ez.Y;
            K.ez.Z = iA + iB;

            if (FrequencyHz > 0.0f)
            {
                Vector2 C1 = cB + rB - cA - rA;

                positionError = C1.Length;
                angularError = 0.0f;

                Vector2 P = -K.Solve22(C1);

                cA -= P * mA;
                aA -= iA * Vector2.Cross(rA, P);

                cB += P * mB;
                aB += iB * Vector2.Cross( rB, P);
            }
            else
            {
                Vector2 C1 = cB + rB - cA - rA;
                float C2 = aB - aA - ReferenceAngle;

                positionError = C1.Length;
                angularError = Math.Abs(C2);

                Vector3 C = new Vector3(C1.X, C1.Y, C2);

                Vector3 impulse;
                if (K.ez.Z <= 0.0f)
                {
                    Vector2 impulse2 = -K.Solve22(C1);
                    impulse = new Vector3(impulse2.X, impulse2.Y, 0.0f);
                }
                else
                {
                    impulse = -K.Solve33(C);
                }
                Vector2 P = new Vector2(impulse.X, impulse.Y);

                cA -= P * mA;
                aA -= iA * (Vector2.Cross( rA, P) + impulse.Z);

                cB += P * mB;
                aB += iB * (Vector2.Cross( rB, P) + impulse.Z);
            }

            data.Positions[_indexA].Center = cA;
            data.Positions[_indexA].Angle = aA;
            data.Positions[_indexB].Center = cB;
            data.Positions[_indexB].Angle = aB;

            return positionError <= PhysicsSettings.LinearSlop && angularError <= PhysicsSettings.AngularSlop;
        }
    }
}
