using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    // Linear constraint (point-to-line)
    // d = pB - pA = xB + rB - xA - rA
    // C = dot(ay, d)
    // Cdot = dot(d, cross(wA, ay)) + dot(ay, vB + cross(wB, rB) - vA - cross(wA, rA))
    //      = -dot(ay, vA) - dot(cross(d + rA, ay), wA) + dot(ay, vB) + dot(cross(rB, ay), vB)
    // J = [-ay, -cross(d + rA, ay), ay, cross(rB, ay)]

    // Spring linear constraint
    // C = dot(ax, d)
    // Cdot = = -dot(ax, vA) - dot(cross(d + rA, ax), wA) + dot(ax, vB) + dot(cross(rB, ax), vB)
    // J = [-ax -cross(d+rA, ax) ax cross(rB, ax)]

    // Motor rotational constraint
    // Cdot = wB - wA
    // J = [0 0 -1 0 0 1]

    /// <summary>
    /// A wheel joint. This joint provides two degrees of freedom: translation
    /// along an axis fixed in bodyA and rotation in the plane. You can use a
    /// joint limit to restrict the range of motion and a joint motor to drive
    /// the rotation or to model rotational friction.
    /// This joint is designed for vehicle suspensions.
    /// </summary>
    public class WheelJoint : Joint
    {
        // Solver shared
        private Vector2 _localXAxis;
        private Vector2 _localYAxis;

        private float _impulse;
        private float _motorImpulse;
        private float _springImpulse;

        private float _maxMotorTorque;
        private float _motorSpeed;
        private bool _enableMotor;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;

        private Vector2 _ax, _ay;
        private float _sAx, _sBx;
        private float _sAy, _sBy;

        private float _mass;
        private float _motorMass;
        private float _springMass;

        private float _bias;
        private float _gamma;
        private Vector2 _axis;

        internal WheelJoint()
        {
            JointType = JointType.Wheel;
        }

        /// <summary>
        /// Constructor for WheelJoint
        /// </summary>
        /// <param name="bodyA">The first body</param>
        /// <param name="bodyB">The second body</param>
        /// <param name="anchor">The anchor point</param>
        /// <param name="axis">The axis</param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public WheelJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchor, Vector2 axis, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Wheel;

            if (useWorldCoordinates)
            {
                LocalAnchorA = bodyA.GetLocalPoint(anchor);
                LocalAnchorB = bodyB.GetLocalPoint(anchor);
            }
            else
            {
                LocalAnchorA = bodyA.GetLocalPoint(bodyB.GetWorldPoint(anchor));
                LocalAnchorB = anchor;
            }

            Axis = axis; //FPE only: We maintain the original value as it is supposed to.
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
        /// The axis at which the suspension moves.
        /// </summary>
        public Vector2 Axis
        {
            get { return _axis; }
            set
            {
                _axis = value;
                _localXAxis = BodyA.GetLocalVector(_axis);
                _localYAxis = Vector2.Rot90(_localXAxis);
            }
        }

        /// <summary>
        /// The axis in local coordinates relative to BodyA
        /// </summary>
        public Vector2 LocalXAxis { get { return _localXAxis; } }

        /// <summary>
        /// The desired motor speed in radians per second.
        /// </summary>
        public float MotorSpeed
        {
            get { return _motorSpeed; }
            set
            {
                WakeBodies();
                _motorSpeed = value;
            }
        }

        /// <summary>
        /// The maximum motor torque, usually in N-m.
        /// </summary>
        public float MaxMotorTorque
        {
            get { return _maxMotorTorque; }
            set
            {
                WakeBodies();
                _maxMotorTorque = value;
            }
        }

        /// <summary>
        /// Suspension frequency, zero indicates no suspension
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// Suspension damping ratio, one indicates critical damping
        /// </summary>
        public float DampingRatio { get; set; }

        /// <summary>
        /// Gets the translation along the axis
        /// </summary>
        public float JointTranslation
        {
            get
            {
                PhysicsComponent bA = BodyA;
                PhysicsComponent bB = BodyB;

                Vector2 pA = bA.GetWorldPoint(LocalAnchorA);
                Vector2 pB = bB.GetWorldPoint(LocalAnchorB);
                Vector2 d = pB - pA;
                Vector2 axis = bA.GetWorldVector(_localXAxis);

                float translation = Vector2.Dot(d, axis);
                return translation;
            }
        }

        /// <summary>
        /// Gets the angular velocity of the joint
        /// </summary>
        public float JointSpeed
        {
            get
            {
                float wA = BodyA.AngularVelocity;
                float wB = BodyB.AngularVelocity;
                return wB - wA;
            }
        }

        /// <summary>
        /// Enable/disable the joint motor.
        /// </summary>
        public bool MotorEnabled
        {
            get => _enableMotor;
            set
            {
                WakeBodies();
                _enableMotor = value;
            }
        }

        /// <summary>
        /// Gets the torque of the motor
        /// </summary>
        /// <param name="invDt">inverse delta time</param>
        public float GetMotorTorque(float invDt)
        {
            return invDt * _motorImpulse;
        }

        public override Vector2 GetReactionForce(float invDt)
        {
            return (_ay * _impulse + _ax * _springImpulse) * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _motorImpulse;
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

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

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

            // Compute the effective masses.
            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);
            Vector2 d1 = cB + rB - cA - rA;

            // Point to line constraint
            {
                _ay = Complex.Multiply(_localYAxis, ref qA);
                _sAy = Vector2.Cross(d1 + rA, _ay);
                _sBy = Vector2.Cross(rB, _ay);

                _mass = mA + mB + iA * _sAy * _sAy + iB * _sBy * _sBy;

                if (_mass > 0.0f)
                {
                    _mass = 1.0f / _mass;
                }
            }

            // Spring constraint
            _springMass = 0.0f;
            _bias = 0.0f;
            _gamma = 0.0f;
            if (Frequency > 0.0f)
            {
                _ax = Complex.Multiply(_localXAxis, ref qA);
                _sAx = Vector2.Cross(d1 + rA, _ax);
                _sBx = Vector2.Cross(rB, _ax);

                float invMass = mA + mB + iA * _sAx * _sAx + iB * _sBx * _sBx;

                if (invMass > 0.0f)
                {
                    _springMass = 1.0f / invMass;

                    float C = Vector2.Dot(d1, _ax);

                    // Frequency
                    // TODO: Tau
                    float omega = 2 * MathF.PI * Frequency;

                    // Damping coefficient
                    float d = 2.0f * _springMass * DampingRatio * omega;

                    // Spring stiffness
                    float k = _springMass * omega * omega;

                    // magic formulas
                    float h = data.Step.DeltaTime;
                    _gamma = h * (d + h * k);
                    if (_gamma > 0.0f)
                    {
                        _gamma = 1.0f / _gamma;
                    }

                    _bias = C * h * k * _gamma;

                    _springMass = invMass + _gamma;
                    if (_springMass > 0.0f)
                    {
                        _springMass = 1.0f / _springMass;
                    }
                }
            }
            else
            {
                _springImpulse = 0.0f;
            }

            // Rotational motor
            if (_enableMotor)
            {
                _motorMass = iA + iB;
                if (_motorMass > 0.0f)
                {
                    _motorMass = 1.0f / _motorMass;
                }
            }
            else
            {
                _motorMass = 0.0f;
                _motorImpulse = 0.0f;
            }

            if (data.Step.WarmStarting)
            {
                // Account for variable time step.
                _impulse *= data.Step.DtRatio;
                _springImpulse *= data.Step.DtRatio;
                _motorImpulse *= data.Step.DtRatio;

                Vector2 P = _ay * _impulse + _ax * _springImpulse;
                float LA = _impulse * _sAy + _springImpulse * _sAx + _motorImpulse;
                float LB = _impulse * _sBy + _springImpulse * _sBx + _motorImpulse;

                vA -= P * _invMassA;
                wA -= _invIA * LA;

                vB += P * _invMassB;
                wB += _invIB * LB;
            }
            else
            {
                _impulse = 0.0f;
                _springImpulse = 0.0f;
                _motorImpulse = 0.0f;
            }

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;
        }

        internal override void SolveVelocityConstraints(ref SolverData data)
        {
            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            Vector2 vA = data.Velocities[_indexA].LinearVelocity;
            float wA = data.Velocities[_indexA].AngularVelocity;
            Vector2 vB = data.Velocities[_indexB].LinearVelocity;
            float wB = data.Velocities[_indexB].AngularVelocity;

            // Solve spring constraint
            {
                float Cdot = Vector2.Dot(_ax, vB - vA) + _sBx * wB - _sAx * wA;
                float impulse = -_springMass * (Cdot + _bias + _gamma * _springImpulse);
                _springImpulse += impulse;

                Vector2 P = _ax * impulse;
                float LA = impulse * _sAx;
                float LB = impulse * _sBx;

                vA -= P * mA;
                wA -= iA * LA;

                vB += P * mB;
                wB += iB * LB;
            }

            // Solve rotational motor constraint
            {
                float Cdot = wB - wA - _motorSpeed;
                float impulse = -_motorMass * Cdot;

                float oldImpulse = _motorImpulse;
                float maxImpulse = data.Step.DeltaTime * _maxMotorTorque;
                _motorImpulse = Math.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _motorImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve point to line constraint
            {
                float Cdot = Vector2.Dot(_ay, vB - vA) + _sBy * wB - _sAy * wA;
                float impulse = -_mass * Cdot;
                _impulse += impulse;

                Vector2 P = _ay * impulse;
                float LA = impulse * _sAy;
                float LB = impulse * _sBy;

                vA -= P * mA;
                wA -= iA * LA;

                vB += P * mB;
                wB += iB * LB;
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

            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);
            Vector2 d = (cB - cA) + rB - rA;

            Vector2 ay = Complex.Multiply(_localYAxis, ref qA);

            float sAy = Vector2.Cross(d + rA, ay);
            float sBy = Vector2.Cross(rB, ay);

            float C = Vector2.Dot(d, ay);

            float k = _invMassA + _invMassB + _invIA * _sAy * _sAy + _invIB * _sBy * _sBy;

            float impulse;
            if (k != 0.0f)
            {
                impulse = -C / k;
            }
            else
            {
                impulse = 0.0f;
            }

            Vector2 P = ay * impulse;
            float LA = impulse * sAy;
            float LB = impulse * sBy;

            cA -= P * _invMassA;
            aA -= _invIA * LA;
            cB += P * _invMassB;
            aB += _invIB * LB;

            data.Positions[_indexA].Center = cA;
            data.Positions[_indexA].Angle = aA;
            data.Positions[_indexB].Center = cB;
            data.Positions[_indexB].Angle = aB;

            return Math.Abs(C) <= PhysicsSettings.LinearSlop;
        }
    }
}
