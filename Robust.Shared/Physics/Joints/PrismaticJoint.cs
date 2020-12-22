using System;
using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    // Linear constraint (point-to-line)
    // d = p2 - p1 = x2 + r2 - x1 - r1
    // C = dot(perp, d)
    // Cdot = dot(d, cross(w1, perp)) + dot(perp, v2 + cross(w2, r2) - v1 - cross(w1, r1))
    //      = -dot(perp, v1) - dot(cross(d + r1, perp), w1) + dot(perp, v2) + dot(cross(r2, perp), v2)
    // J = [-perp, -cross(d + r1, perp), perp, cross(r2,perp)]
    //
    // Angular constraint
    // C = a2 - a1 + a_initial
    // Cdot = w2 - w1
    // J = [0 0 -1 0 0 1]
    //
    // K = J * invM * JT
    //
    // J = [-a -s1 a s2]
    //     [0  -1  0  1]
    // a = perp
    // s1 = cross(d + r1, a) = cross(p2 - x1, a)
    // s2 = cross(r2, a) = cross(p2 - x2, a)
    // Motor/Limit linear constraint
    // C = dot(ax1, d)
    // Cdot = = -dot(ax1, v1) - dot(cross(d + r1, ax1), w1) + dot(ax1, v2) + dot(cross(r2, ax1), v2)
    // J = [-ax1 -cross(d+r1,ax1) ax1 cross(r2,ax1)]
    // Block Solver
    // We develop a block solver that includes the joint limit. This makes the limit stiff (inelastic) even
    // when the mass has poor distribution (leading to large torques about the joint anchor points).
    //
    // The Jacobian has 3 rows:
    // J = [-uT -s1 uT s2] // linear
    //     [0   -1   0  1] // angular
    //     [-vT -a1 vT a2] // limit
    //
    // u = perp
    // v = axis
    // s1 = cross(d + r1, u), s2 = cross(r2, u)
    // a1 = cross(d + r1, v), a2 = cross(r2, v)
    // M * (v2 - v1) = JT * df
    // J * v2 = bias
    //
    // v2 = v1 + invM * JT * df
    // J * (v1 + invM * JT * df) = bias
    // K * df = bias - J * v1 = -Cdot
    // K = J * invM * JT
    // Cdot = J * v1 - bias
    //
    // Now solve for f2.
    // df = f2 - f1
    // K * (f2 - f1) = -Cdot
    // f2 = invK * (-Cdot) + f1
    //
    // Clamp accumulated limit impulse.
    // lower: f2(3) = max(f2(3), 0)
    // upper: f2(3) = min(f2(3), 0)
    //
    // Solve for correct f2(1:2)
    // K(1:2, 1:2) * f2(1:2) = -Cdot(1:2) - K(1:2,3) * f2(3) + K(1:2,1:3) * f1
    //                       = -Cdot(1:2) - K(1:2,3) * f2(3) + K(1:2,1:2) * f1(1:2) + K(1:2,3) * f1(3)
    // K(1:2, 1:2) * f2(1:2) = -Cdot(1:2) - K(1:2,3) * (f2(3) - f1(3)) + K(1:2,1:2) * f1(1:2)
    // f2(1:2) = invK(1:2,1:2) * (-Cdot(1:2) - K(1:2,3) * (f2(3) - f1(3))) + f1(1:2)
    //
    // Now compute impulse to be applied:
    // df = f2 - f1

    /// <summary>
    /// A prismatic joint. This joint provides one degree of freedom: translation
    /// along an axis fixed in bodyA. Relative rotation is prevented. You can
    /// use a joint limit to restrict the range of motion and a joint motor to
    /// drive the motion or to model joint friction.
    /// </summary>
    public class PrismaticJoint : Joint
    {
        private Vector2 _localXAxis;
        private Vector2 _localYAxisA;
        private Vector3 _impulse;
        private float _lowerTranslation;
        private float _upperTranslation;
        private float _maxMotorForce;
        private float _motorSpeed;
        private bool _enableLimit;
        private bool _enableMotor;
        private LimitState _limitState;

        // Solver temp
        private int _indexA;
        private int _indexB;
        private Vector2 _localCenterA;
        private Vector2 _localCenterB;
        private float _invMassA;
        private float _invMassB;
        private float _invIA;
        private float _invIB;
        private Vector2 _axis, _perp;
        private float _s1, _s2;
        private float _a1, _a2;
        private Mat33 _K;
        private float _motorMass;
        private Vector2 _axis1;

        internal PrismaticJoint()
        {
            JointType = JointType.Prismatic;
        }

        /// <summary>
        /// This requires defining a line of
        /// motion using an axis and an anchor point. The definition uses local
        /// anchor points and a local axis so that the initial configuration
        /// can violate the constraint slightly. The joint translation is zero
        /// when the local anchor points coincide in world space. Using local
        /// anchors and a local axis helps when saving and loading a game.
        /// </summary>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="anchorA">The first body anchor.</param>
        /// <param name="anchorB">The second body anchor.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public PrismaticJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchorA, Vector2 anchorB, Vector2 axis, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            Initialize(anchorA, anchorB, axis, useWorldCoordinates);
        }

        public PrismaticJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchor, Vector2 axis, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
            Initialize(anchor, anchor, axis, useWorldCoordinates);
        }

        private void Initialize(Vector2 localAnchorA, Vector2 localAnchorB, Vector2 axis, bool useWorldCoordinates)
        {
            JointType = JointType.Prismatic;

            if (useWorldCoordinates)
            {
                LocalAnchorA = BodyA.GetLocalPoint(localAnchorA);
                LocalAnchorB = BodyB.GetLocalPoint(localAnchorB);
            }
            else
            {
                LocalAnchorA = localAnchorA;
                LocalAnchorB = localAnchorB;
            }

            Axis = axis; //FPE only: store the orignal value for use in Serialization
            ReferenceAngle = BodyB.Rotation - BodyA.Rotation;

            _limitState = LimitState.Inactive;
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
        /// Get the current joint translation, usually in meters.
        /// </summary>
        /// <value></value>
        public float JointTranslation
        {
            get
            {
                Vector2 d = BodyB.GetWorldPoint(LocalAnchorB) - BodyA.GetWorldPoint(LocalAnchorA);
                Vector2 axis = BodyA.GetWorldVector(_localXAxis);

                return Vector2.Dot(d, axis);
            }
        }

        /// <summary>
        /// Get the current joint translation speed, usually in meters per second.
        /// </summary>
        /// <value></value>
        public float JointSpeed
        {
            get
            {
                PhysicsTransform xf1 = BodyA.GetTransform();
                PhysicsTransform xf2 = BodyB.GetTransform();

                Vector2 r1 = Complex.Multiply(LocalAnchorA - BodyA.LocalCenter, ref xf1.Quaternion);
                Vector2 r2 = Complex.Multiply(LocalAnchorB - BodyB.LocalCenter, ref xf2.Quaternion);
                Vector2 p1 = BodyA.Sweep.Center + r1;
                Vector2 p2 = BodyB.Sweep.Center + r2;
                Vector2 d = p2 - p1;
                Vector2 axis = BodyA.GetWorldVector(_localXAxis);

                Vector2 v1 = BodyA.LinearVelocity;
                Vector2 v2 = BodyB.LinearVelocity;
                float w1 = BodyA.AngularVelocity;
                float w2 = BodyB.AngularVelocity;

                float speed = Vector2.Dot(d, Vector2.Cross(w1, axis)) + Vector2.Dot(axis, v2 + Vector2.Cross(w2, r2) - v1 - Vector2.Cross(w1, r1));
                return speed;
            }
        }

        /// <summary>
        /// Is the joint limit enabled?
        /// </summary>
        /// <value><c>true</c> if [limit enabled]; otherwise, <c>false</c>.</value>
        public bool LimitEnabled
        {
            get { return _enableLimit; }
            set
            {
                Debug.Assert(!BodyA.FixedRotation || !BodyB.FixedRotation, "Warning: limits does currently not work with fixed rotation");

                if (value != _enableLimit)
                {
                    WakeBodies();
                    _enableLimit = value;
                    _impulse.Z = 0;
                }
            }
        }

        /// <summary>
        /// Get the lower joint limit, usually in meters.
        /// </summary>
        /// <value></value>
        public float LowerLimit
        {
            get { return _lowerTranslation; }
            set
            {
                if (value != _lowerTranslation)
                {
                    WakeBodies();
                    _lowerTranslation = value;
                    _impulse.Z = 0.0f;
                }
            }
        }

        /// <summary>
        /// Get the upper joint limit, usually in meters.
        /// </summary>
        /// <value></value>
        public float UpperLimit
        {
            get { return _upperTranslation; }
            set
            {
                if (value != _upperTranslation)
                {
                    WakeBodies();
                    _upperTranslation = value;
                    _impulse.Z = 0.0f;
                }
            }
        }

        /// <summary>
        /// Set the joint limits, usually in meters.
        /// </summary>
        /// <param name="lower">The lower limit</param>
        /// <param name="upper">The upper limit</param>
        public void SetLimits(float lower, float upper)
        {
            if (upper != _upperTranslation || lower != _lowerTranslation)
            {
                WakeBodies();
                _upperTranslation = upper;
                _lowerTranslation = lower;
                _impulse.Z = 0.0f;
            }
        }

        /// <summary>
        /// Is the joint motor enabled?
        /// </summary>
        /// <value><c>true</c> if [motor enabled]; otherwise, <c>false</c>.</value>
        public bool MotorEnabled
        {
            get { return _enableMotor; }
            set
            {
                WakeBodies();
                _enableMotor = value;
            }
        }

        /// <summary>
        /// Set the motor speed, usually in meters per second.
        /// </summary>
        /// <value>The speed.</value>
        public float MotorSpeed
        {
            set
            {
                WakeBodies();
                _motorSpeed = value;
            }
            get { return _motorSpeed; }
        }

        /// <summary>
        /// Set the maximum motor force, usually in N.
        /// </summary>
        /// <value>The force.</value>
        public float MaxMotorForce
        {
            get { return _maxMotorForce; }
            set
            {
                WakeBodies();
                _maxMotorForce = value;
            }
        }

        /// <summary>
        /// Get the current motor impulse, usually in N.
        /// </summary>
        /// <value></value>
        public float MotorImpulse { get; set; }

        /// <summary>
        /// Gets the motor force.
        /// </summary>
        /// <param name="invDt">The inverse delta time</param>
        public float GetMotorForce(float invDt)
        {
            return invDt * MotorImpulse;
        }

        /// <summary>
        /// The axis at which the joint moves.
        /// </summary>
        public Vector2 Axis
        {
            get { return _axis1; }
            set
            {
                _axis1 = value;
                _localXAxis = BodyA.GetLocalVector(_axis1);
                _localXAxis = _localXAxis.Normalized;
                _localYAxisA = Vector2.Cross(1.0f, _localXAxis);
            }
        }

        /// <summary>
        /// The axis in local coordinates relative to BodyA
        /// </summary>
        public Vector2 LocalXAxis { get { return _localXAxis; } }

        /// <summary>
        /// The reference angle.
        /// </summary>
        public float ReferenceAngle { get; set; }

        public override Vector2 GetReactionForce(float invDt)
        {
            return (_perp * _impulse.X + _axis * (MotorImpulse + _impulse.Z)) * invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            return invDt * _impulse.Y;
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

            // Compute the effective masses.
            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);
            Vector2 d = (cB - cA) + rB - rA;

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            // Compute motor Jacobian and effective mass.
            {
                _axis = Complex.Multiply(_localXAxis, ref qA);
                _a1 = Vector2.Cross(d + rA, _axis);
                _a2 = Vector2.Cross(rB, _axis);

                _motorMass = mA + mB + iA * _a1 * _a1 + iB * _a2 * _a2;
                if (_motorMass > 0.0f)
                {
                    _motorMass = 1.0f / _motorMass;
                }
            }

            // Prismatic constraint.
            {
                _perp = Complex.Multiply(_localYAxisA, ref qA);

                _s1 = Vector2.Cross(d + rA, _perp);
                _s2 = Vector2.Cross(rB, _perp);

                float k11 = mA + mB + iA * _s1 * _s1 + iB * _s2 * _s2;
                float k12 = iA * _s1 + iB * _s2;
                float k13 = iA * _s1 * _a1 + iB * _s2 * _a2;
                float k22 = iA + iB;
                if (k22 == 0.0f)
                {
                    // For bodies with fixed rotation.
                    k22 = 1.0f;
                }
                float k23 = iA * _a1 + iB * _a2;
                float k33 = mA + mB + iA * _a1 * _a1 + iB * _a2 * _a2;

                _K.ex = new Vector3(k11, k12, k13);
                _K.ey = new Vector3(k12, k22, k23);
                _K.ez = new Vector3(k13, k23, k33);
            }

            // Compute motor and limit terms.
            if (_enableLimit)
            {
                float jointTranslation = Vector2.Dot(_axis, d);
                if (Math.Abs(_upperTranslation - _lowerTranslation) < 2.0f * PhysicsSettings.LinearSlop)
                {
                    _limitState = LimitState.Equal;
                }
                else if (jointTranslation <= _lowerTranslation)
                {
                    if (_limitState != LimitState.AtLower)
                    {
                        _limitState = LimitState.AtLower;
                        _impulse.Z = 0.0f;
                    }
                }
                else if (jointTranslation >= _upperTranslation)
                {
                    if (_limitState != LimitState.AtUpper)
                    {
                        _limitState = LimitState.AtUpper;
                        _impulse.Z = 0.0f;
                    }
                }
                else
                {
                    _limitState = LimitState.Inactive;
                    _impulse.Z = 0.0f;
                }
            }
            else
            {
                _limitState = LimitState.Inactive;
                _impulse.Z = 0.0f;
            }

            if (_enableMotor == false)
            {
                MotorImpulse = 0.0f;
            }

            if (data.Step.WarmStarting)
            {
                // Account for variable time step.
                _impulse *= data.Step.DtRatio;
                MotorImpulse *= data.Step.DtRatio;

                Vector2 P = _perp * _impulse.X + _axis * (MotorImpulse + _impulse.Z);
                float LA = _impulse.X * _s1 + _impulse.Y + (MotorImpulse + _impulse.Z) * _a1;
                float LB = _impulse.X * _s2 + _impulse.Y + (MotorImpulse + _impulse.Z) * _a2;

                vA -= P * mA;
                wA -= iA * LA;

                vB += P * mB;
                wB += iB * LB;
            }
            else
            {
                _impulse = Vector3.Zero;
                MotorImpulse = 0.0f;
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

            // Solve linear motor constraint.
            if (_enableMotor && _limitState != LimitState.Equal)
            {
                float Cdot = Vector2.Dot(_axis, vB - vA) + _a2 * wB - _a1 * wA;
                float impulse = _motorMass * (_motorSpeed - Cdot);
                float oldImpulse = MotorImpulse;
                float maxImpulse = data.Step.DeltaTime * _maxMotorForce;
                MotorImpulse = Math.Clamp(MotorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = MotorImpulse - oldImpulse;

                Vector2 P = _axis * impulse;
                float LA = impulse * _a1;
                float LB = impulse * _a2;

                vA -= P * mA;
                wA -= iA * LA;

                vB += P * mB;
                wB += iB * LB;
            }

            Vector2 Cdot1 = new Vector2();
            Cdot1.X = Vector2.Dot(_perp, vB - vA) + _s2 * wB - _s1 * wA;
            Cdot1.Y = wB - wA;

            if (_enableLimit && _limitState != LimitState.Inactive)
            {
                // Solve prismatic and limit constraint in block form.
                float Cdot2;
                Cdot2 = Vector2.Dot(_axis, vB - vA) + _a2 * wB - _a1 * wA;
                Vector3 Cdot = new Vector3(Cdot1.X, Cdot1.Y, Cdot2);

                Vector3 f1 = _impulse;
                Vector3 df = _K.Solve33(-Cdot);
                _impulse += df;

                if (_limitState == LimitState.AtLower)
                {
                    _impulse.Z = Math.Max(_impulse.Z, 0.0f);
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    _impulse.Z = Math.Min(_impulse.Z, 0.0f);
                }

                // f2(1:2) = invK(1:2,1:2) * (-Cdot(1:2) - K(1:2,3) * (f2(3) - f1(3))) + f1(1:2)
                Vector2 b = -Cdot1 - new Vector2(_K.ez.X, _K.ez.Y) * (_impulse.Z - f1.Z);
                Vector2 f2r = _K.Solve22(b) + new Vector2(f1.X, f1.Y);
                _impulse.X = f2r.X;
                _impulse.Y = f2r.Y;

                df = _impulse - f1;

                Vector2 P = _perp * df.X + _axis * df.Z;
                float LA = df.X * _s1 + df.Y + df.Z * _a1;
                float LB = df.X * _s2 + df.Y + df.Z * _a2;

                vA -= P * mA;
                wA -= iA * LA;

                vB += P * mB;
                wB += iB * LB;
            }
            else
            {
                // Limit is inactive, just solve the prismatic constraint in block form.
                Vector2 df = _K.Solve22(-Cdot1);
                _impulse.X += df.X;
                _impulse.Y += df.Y;

                Vector2 P = _perp * df.X;
                float LA = df.X * _s1 + df.Y;
                float LB = df.X * _s2 + df.Y;

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

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            // Compute fresh Jacobians
            Vector2 rA = Complex.Multiply(LocalAnchorA - _localCenterA, ref qA);
            Vector2 rB = Complex.Multiply(LocalAnchorB - _localCenterB, ref qB);
            Vector2 d = cB + rB - cA - rA;

            Vector2 axis = Complex.Multiply(_localXAxis, ref qA);
            float a1 = Vector2.Cross(d + rA, axis);
            float a2 = Vector2.Cross(rB, axis);
            Vector2 perp = Complex.Multiply(_localYAxisA, ref qA);

            float s1 = Vector2.Cross(d + rA, perp);
            float s2 = Vector2.Cross(rB, perp);

            Vector3 impulse;
            Vector2 C1 = new Vector2();
            C1.X = Vector2.Dot(perp, d);
            C1.Y = aB - aA - ReferenceAngle;

            float linearError = Math.Abs(C1.X);
            float angularError = Math.Abs(C1.Y);

            bool active = false;
            float C2 = 0.0f;
            if (_enableLimit)
            {
                float translation = Vector2.Dot(axis, d);
                if (Math.Abs(_upperTranslation - _lowerTranslation) < 2.0f * PhysicsSettings.LinearSlop)
                {
                    // Prevent large angular corrections
                    C2 = Math.Clamp(translation, -PhysicsSettings.MaxLinearCorrection, PhysicsSettings.MaxLinearCorrection);
                    linearError = Math.Max(linearError, Math.Abs(translation));
                    active = true;
                }
                else if (translation <= _lowerTranslation)
                {
                    // Prevent large linear corrections and allow some slop.
                    C2 = Math.Clamp(translation - _lowerTranslation + PhysicsSettings.LinearSlop, -PhysicsSettings.MaxLinearCorrection, 0.0f);
                    linearError = Math.Max(linearError, _lowerTranslation - translation);
                    active = true;
                }
                else if (translation >= _upperTranslation)
                {
                    // Prevent large linear corrections and allow some slop.
                    C2 = Math.Clamp(translation - _upperTranslation - PhysicsSettings.LinearSlop, 0.0f, PhysicsSettings.MaxLinearCorrection);
                    linearError = Math.Max(linearError, translation - _upperTranslation);
                    active = true;
                }
            }

            if (active)
            {
                float k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
                float k12 = iA * s1 + iB * s2;
                float k13 = iA * s1 * a1 + iB * s2 * a2;
                float k22 = iA + iB;
                if (k22 == 0.0f)
                {
                    // For fixed rotation
                    k22 = 1.0f;
                }
                float k23 = iA * a1 + iB * a2;
                float k33 = mA + mB + iA * a1 * a1 + iB * a2 * a2;

                Mat33 K = new Mat33();
                K.ex = new Vector3(k11, k12, k13);
                K.ey = new Vector3(k12, k22, k23);
                K.ez = new Vector3(k13, k23, k33);

                Vector3 C = new Vector3();
                C.X = C1.X;
                C.Y = C1.Y;
                C.Z = C2;

                impulse = K.Solve33(-C);
            }
            else
            {
                float k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
                float k12 = iA * s1 + iB * s2;
                float k22 = iA + iB;
                if (k22 == 0.0f)
                {
                    k22 = 1.0f;
                }

                Mat22 K = new Mat22();
                K.ex = new Vector2(k11, k12);
                K.ey = new Vector2(k12, k22);

                Vector2 impulse1 = K.Solve(-C1);
                impulse = new Vector3();
                impulse.X = impulse1.X;
                impulse.Y = impulse1.Y;
                impulse.Z = 0.0f;
            }

            Vector2 P = perp * impulse.X + axis * impulse.Z;
            float LA = impulse.X * s1 + impulse.Y + impulse.Z * a1;
            float LB = impulse.X * s2 + impulse.Y + impulse.Z * a2;

            cA -= P * mA;
            aA -= iA * LA;
            cB += P * mB;
            aB += iB * LB;

            data.Positions[_indexA].Center = cA;
            data.Positions[_indexA].Angle = aA;
            data.Positions[_indexB].Center = cB;
            data.Positions[_indexB].Angle = aB;

            return linearError <= PhysicsSettings.LinearSlop && angularError <= PhysicsSettings.AngularSlop;
        }
    }
}
