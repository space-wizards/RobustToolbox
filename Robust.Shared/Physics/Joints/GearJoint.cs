using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Joints
{
    // Gear Joint:
    // C0 = (coordinate1 + ratio * coordinate2)_initial
    // C = (coordinate1 + ratio * coordinate2) - C0 = 0
    // J = [J1 ratio * J2]
    // K = J * invM * JT
    //   = J1 * invM1 * J1T + ratio * ratio * J2 * invM2 * J2T
    //
    // Revolute:
    // coordinate = rotation
    // Cdot = angularVelocity
    // J = [0 0 1]
    // K = J * invM * JT = invI
    //
    // Prismatic:
    // coordinate = dot(p - pg, ug)
    // Cdot = dot(v + cross(w, r), ug)
    // J = [ug cross(r, ug)]
    // K = J * invM * JT = invMass + invI * cross(r, ug)^2

    /// <summary>
    /// A gear joint is used to connect two joints together.
    /// Either joint can be a revolute or prismatic joint.
    /// You specify a gear ratio to bind the motions together:
    /// <![CDATA[coordinate1 + ratio * coordinate2 = ant]]>
    /// The ratio can be negative or positive. If one joint is a revolute joint
    /// and the other joint is a prismatic joint, then the ratio will have units
    /// of length or units of 1/length.
    ///
    /// Warning: You have to manually destroy the gear joint if jointA or jointB is destroyed.
    /// </summary>
    public sealed class GearJoint : Joint
    {
        private JointType _typeA;
        private JointType _typeB;

        private PhysicsComponent _bodyA;
        private PhysicsComponent _bodyB;
        private PhysicsComponent _bodyC;
        private PhysicsComponent _bodyD;

        // Solver shared
        private Vector2 _localAnchorA;
        private Vector2 _localAnchorB;
        private Vector2 _localAnchorC;
        private Vector2 _localAnchorD;

        private Vector2 _localAxisC;
        private Vector2 _localAxisD;

        private float _referenceAngleA;
        private float _referenceAngleB;

        private float _constant;
        private float _ratio;

        private float _impulse;

        // Solver temp
        private int _indexA, _indexB, _indexC, _indexD;
        private Vector2 _lcA, _lcB, _lcC, _lcD;
        private float _mA, _mB, _mC, _mD;
        private float _iA, _iB, _iC, _iD;
        private Vector2 _JvAC, _JvBD;
        private float _JwA, _JwB, _JwC, _JwD;
        private float _mass;

        /// <summary>
        /// Requires two existing revolute or prismatic joints (any combination will work).
        /// The provided joints must attach a dynamic body to a static body.
        /// </summary>
        /// <param name="jointA">The first joint.</param>
        /// <param name="jointB">The second joint.</param>
        /// <param name="ratio">The ratio.</param>
        /// <param name="bodyA">The first body</param>
        /// <param name="bodyB">The second body</param>
        public GearJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Joint jointA, Joint jointB, float ratio = 1f)
        {
            JointType = JointType.Gear;
            BodyA = bodyA;
            BodyB = bodyB;
            JointA = jointA;
            JointB = jointB;
            Ratio = ratio;

            _typeA = jointA.JointType;
            _typeB = jointB.JointType;

            Debug.Assert(_typeA == JointType.Revolute || _typeA == JointType.Prismatic || _typeA == JointType.FixedRevolute || _typeA == JointType.FixedPrismatic);
            Debug.Assert(_typeB == JointType.Revolute || _typeB == JointType.Prismatic || _typeB == JointType.FixedRevolute || _typeB == JointType.FixedPrismatic);

            float coordinateA, coordinateB;

            // TODO_ERIN there might be some problem with the joint edges in b2Joint.

            _bodyC = JointA.BodyA;
            _bodyA = JointA.BodyB;

            // Get geometry of joint1
            PhysicsTransform xfA = _bodyA.GetTransform();
            float aA = _bodyA.Sweep.Angle;
            PhysicsTransform xfC = _bodyC.GetTransform();
            float aC = _bodyC.Sweep.Angle;

            if (_typeA == JointType.Revolute)
            {
                RevoluteJoint revolute = (RevoluteJoint) jointA;
                _localAnchorC = revolute.LocalAnchorA;
                _localAnchorA = revolute.LocalAnchorB;
                _referenceAngleA = revolute.ReferenceAngle;
                _localAxisC = Vector2.Zero;

                coordinateA = aA - aC - _referenceAngleA;
            }
            else
            {
                PrismaticJoint prismatic = (PrismaticJoint) jointA;
                _localAnchorC = prismatic.LocalAnchorA;
                _localAnchorA = prismatic.LocalAnchorB;
                _referenceAngleA = prismatic.ReferenceAngle;
                _localAxisC = prismatic.LocalXAxis;

                Vector2 pC = _localAnchorC;
                Vector2 pA = Complex.Divide(Complex.Multiply(_localAnchorA, ref xfA.Quaternion) + (xfA.Position - xfC.Position), ref xfC.Quaternion);
                coordinateA = Vector2.Dot(pA - pC, _localAxisC);
            }

            _bodyD = JointB.BodyA;
            _bodyB = JointB.BodyB;

            // Get geometry of joint2
            PhysicsTransform xfB = _bodyB.GetTransform();
            float aB = _bodyB.Sweep.Angle;
            PhysicsTransform xfD = _bodyD.GetTransform();
            float aD = _bodyD.Sweep.Angle;

            if (_typeB == JointType.Revolute)
            {
                RevoluteJoint revolute = (RevoluteJoint) jointB;
                _localAnchorD = revolute.LocalAnchorA;
                _localAnchorB = revolute.LocalAnchorB;
                _referenceAngleB = revolute.ReferenceAngle;
                _localAxisD = Vector2.Zero;

                coordinateB = aB - aD - _referenceAngleB;
            }
            else
            {
                PrismaticJoint prismatic = (PrismaticJoint) jointB;
                _localAnchorD = prismatic.LocalAnchorA;
                _localAnchorB = prismatic.LocalAnchorB;
                _referenceAngleB = prismatic.ReferenceAngle;
                _localAxisD = prismatic.LocalXAxis;

                Vector2 pD = _localAnchorD;
                Vector2 pB = Complex.Divide(Complex.Multiply(_localAnchorB, ref xfB.Quaternion) + (xfB.Position - xfD.Position), ref xfD.Quaternion);
                coordinateB = Vector2.Dot(pB - pD, _localAxisD);
            }

            _ratio = ratio;
            _constant = coordinateA + _ratio * coordinateB;
            _impulse = 0.0f;
        }

        public override Vector2 WorldAnchorA
        {
            get { return _bodyA.GetWorldPoint(_localAnchorA); }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        public override Vector2 WorldAnchorB
        {
            get { return _bodyB.GetWorldPoint(_localAnchorB); }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        /// <summary>
        /// The gear ratio.
        /// </summary>
        public float Ratio
        {
            get { return _ratio; }
            set
            {
                DebugTools.Assert(!float.IsNaN(value));
                _ratio = value;
            }
        }

        /// <summary>
        /// The first revolute/prismatic joint attached to the gear joint.
        /// </summary>
        public Joint JointA { get; private set; }

        /// <summary>
        /// The second revolute/prismatic joint attached to the gear joint.
        /// </summary>
        public Joint JointB { get; private set; }

        public override Vector2 GetReactionForce(float invDt)
        {
            Vector2 P = _JvAC * _impulse;
            return P *invDt;
        }

        public override float GetReactionTorque(float invDt)
        {
            float L = _impulse * _JwA;
            return invDt * L;
        }

        internal override void InitVelocityConstraints(ref SolverData data)
        {
            _indexA = _bodyA.IslandIndex;
            _indexB = _bodyB.IslandIndex;
            _indexC = _bodyC.IslandIndex;
            _indexD = _bodyD.IslandIndex;
            _lcA = _bodyA.Sweep.LocalCenter;
            _lcB = _bodyB.Sweep.LocalCenter;
            _lcC = _bodyC.Sweep.LocalCenter;
            _lcD = _bodyD.Sweep.LocalCenter;
            _mA = _bodyA.InvMass;
            _mB = _bodyB.InvMass;
            _mC = _bodyC.InvMass;
            _mD = _bodyD.InvMass;
            _iA = _bodyA.InvI;
            _iB = _bodyB.InvI;
            _iC = _bodyC.InvI;
            _iD = _bodyD.InvI;

            float aA = data.Positions[_indexA].Angle;
            Vector2 vA = data.Velocities[_indexA].LinearVelocity;
            float wA = data.Velocities[_indexA].AngularVelocity;

            float aB = data.Positions[_indexB].Angle;
            Vector2 vB = data.Velocities[_indexB].LinearVelocity;
            float wB = data.Velocities[_indexB].AngularVelocity;

            float aC = data.Positions[_indexC].Angle;
            Vector2 vC = data.Velocities[_indexC].LinearVelocity;
            float wC = data.Velocities[_indexC].AngularVelocity;

            float aD = data.Positions[_indexD].Angle;
            Vector2 vD = data.Velocities[_indexD].LinearVelocity;
            float wD = data.Velocities[_indexD].AngularVelocity;

            Complex qA = Complex.FromAngle(aA);
            Complex qB = Complex.FromAngle(aB);
            Complex qC = Complex.FromAngle(aC);
            Complex qD = Complex.FromAngle(aD);

            _mass = 0.0f;

            if (_typeA == JointType.Revolute)
            {
                _JvAC = Vector2.Zero;
                _JwA = 1.0f;
                _JwC = 1.0f;
                _mass += _iA + _iC;
            }
            else
            {
                Vector2 u = Complex.Multiply(_localAxisC, ref qC);
                Vector2 rC = Complex.Multiply(_localAnchorC - _lcC, ref qC);
                Vector2 rA = Complex.Multiply(_localAnchorA - _lcA, ref qA);
                _JvAC = u;
                _JwC = Vector2.Cross(rC, u);
                _JwA = Vector2.Cross(rA, u);
                _mass += _mC + _mA + _iC * _JwC * _JwC + _iA * _JwA * _JwA;
            }

            if (_typeB == JointType.Revolute)
            {
                _JvBD = Vector2.Zero;
                _JwB = _ratio;
                _JwD = _ratio;
                _mass += _ratio * _ratio * (_iB + _iD);
            }
            else
            {
                Vector2 u = Complex.Multiply(_localAxisD, ref qD);
                Vector2 rD = Complex.Multiply(_localAnchorD - _lcD, ref qD);
                Vector2 rB = Complex.Multiply(_localAnchorB - _lcB, ref qB);
                _JvBD = u * _ratio;
                _JwD = _ratio * Vector2.Cross(rD, u);
                _JwB = _ratio * Vector2.Cross(rB, u);
                _mass += _ratio * _ratio * (_mD + _mB) + _iD * _JwD * _JwD + _iB * _JwB * _JwB;
            }

            // Compute effective mass.
            _mass = _mass > 0.0f ? 1.0f / _mass : 0.0f;

            if (data.Step.WarmStarting)
            {
                vA += _JvAC * (_mA * _impulse);
                wA += _iA * _impulse * _JwA;
                vB += _JvBD * (_mB * _impulse);
                wB += _iB * _impulse * _JwB;
                vC -= _JvAC * (_mC * _impulse);
                wC -= _iC * _impulse * _JwC;
                vD -= _JvBD * (_mD * _impulse);
                wD -= _iD * _impulse * _JwD;
            }
            else
            {
                _impulse = 0.0f;
            }

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;
            data.Velocities[_indexC].LinearVelocity = vC;
            data.Velocities[_indexC].AngularVelocity = wC;
            data.Velocities[_indexD].LinearVelocity = vD;
            data.Velocities[_indexD].AngularVelocity = wD;
        }

        internal override void SolveVelocityConstraints(ref SolverData data)
        {
            Vector2 vA = data.Velocities[_indexA].LinearVelocity;
            float wA = data.Velocities[_indexA].AngularVelocity;
            Vector2 vB = data.Velocities[_indexB].LinearVelocity;
            float wB = data.Velocities[_indexB].AngularVelocity;
            Vector2 vC = data.Velocities[_indexC].LinearVelocity;
            float wC = data.Velocities[_indexC].AngularVelocity;
            Vector2 vD = data.Velocities[_indexD].LinearVelocity;
            float wD = data.Velocities[_indexD].AngularVelocity;

            float Cdot = Vector2.Dot(_JvAC, vA - vC) + Vector2.Dot(_JvBD, vB - vD);
            Cdot += (_JwA * wA - _JwC * wC) + (_JwB * wB - _JwD * wD);

            float impulse = -_mass * Cdot;
            _impulse += impulse;

            vA += _JvAC * (_mA * impulse);
            wA += _JwA * _iA * impulse;
            vB += _JvBD * (_mB * impulse);
            wB += _iB * impulse * _JwB;
            vC -= _JvAC * (_mC * impulse);
            wC -= _iC * impulse * _JwC;
            vD -= _JvBD * (_mD * impulse);
            wD -= _iD * impulse * _JwD;

            data.Velocities[_indexA].LinearVelocity = vA;
            data.Velocities[_indexA].AngularVelocity = wA;
            data.Velocities[_indexB].LinearVelocity = vB;
            data.Velocities[_indexB].AngularVelocity = wB;
            data.Velocities[_indexC].LinearVelocity = vC;
            data.Velocities[_indexC].AngularVelocity = wC;
            data.Velocities[_indexD].LinearVelocity = vD;
            data.Velocities[_indexD].AngularVelocity = wD;
        }

        internal override bool SolvePositionConstraints(ref SolverData data)
        {
            Vector2 cA = data.Positions[_indexA].Center;
            float aA = data.Positions[_indexA].Angle;
            Vector2 cB = data.Positions[_indexB].Center;
            float aB = data.Positions[_indexB].Angle;
            Vector2 cC = data.Positions[_indexC].Center;
            float aC = data.Positions[_indexC].Angle;
            Vector2 cD = data.Positions[_indexD].Center;
            float aD = data.Positions[_indexD].Angle;

            Complex qA = Complex.FromAngle(aA);
            Complex qB = Complex.FromAngle(aB);
            Complex qC = Complex.FromAngle(aC);
            Complex qD = Complex.FromAngle(aD);

            const float linearError = 0.0f;

            float coordinateA, coordinateB;

            Vector2 JvAC, JvBD;
            float JwA, JwB, JwC, JwD;
            float mass = 0.0f;

            if (_typeA == JointType.Revolute)
            {
                JvAC = Vector2.Zero;
                JwA = 1.0f;
                JwC = 1.0f;
                mass += _iA + _iC;

                coordinateA = aA - aC - _referenceAngleA;
            }
            else
            {
                Vector2 u = Complex.Multiply(_localAxisC, ref qC);
                Vector2 rC = Complex.Multiply(_localAnchorC - _lcC, ref qC);
                Vector2 rA = Complex.Multiply(_localAnchorA - _lcA, ref qA);
                JvAC = u;
                JwC = Vector2.Cross(rC, u);
                JwA = Vector2.Cross(rA, u);
                mass += _mC + _mA + _iC * JwC * JwC + _iA * JwA * JwA;

                Vector2 pC = _localAnchorC - _lcC;
                Vector2 pA = Complex.Divide(rA + (cA - cC), ref qC);
                coordinateA = Vector2.Dot(pA - pC, _localAxisC);
            }

            if (_typeB == JointType.Revolute)
            {
                JvBD = Vector2.Zero;
                JwB = _ratio;
                JwD = _ratio;
                mass += _ratio * _ratio * (_iB + _iD);

                coordinateB = aB - aD - _referenceAngleB;
            }
            else
            {
                Vector2 u = Complex.Multiply(_localAxisD, ref qD);
                Vector2 rD = Complex.Multiply(_localAnchorD - _lcD, ref qD);
                Vector2 rB = Complex.Multiply(_localAnchorB - _lcB, ref qB);
                JvBD = u * _ratio;
                JwD = _ratio * Vector2.Cross(rD, u);
                JwB = _ratio * Vector2.Cross(rB, u);
                mass += _ratio * _ratio * (_mD + _mB) + _iD * JwD * JwD + _iB * JwB * JwB;

                Vector2 pD = _localAnchorD - _lcD;
                Vector2 pB = Complex.Divide(rB + (cB - cD), ref qD);
                coordinateB = Vector2.Dot(pB - pD, _localAxisD);
            }

            float C = (coordinateA + _ratio * coordinateB) - _constant;

            float impulse = 0.0f;
            if (mass > 0.0f)
            {
                impulse = -C / mass;
            }

            cA += JvAC * _mA * impulse;
            aA += JwA * _iA * impulse;
            cB += JvBD * _mB * impulse;
            aB += JwB * _iB * impulse;
            cC -= JvAC * _mC * impulse;
            aC -= JwC * _iC * impulse;
            cD -= JvBD * _mD * impulse;
            aD -= JwD * _iD * impulse;

            data.Positions[_indexA].Center = cA;
            data.Positions[_indexA].Angle = aA;
            data.Positions[_indexB].Center = cB;
            data.Positions[_indexB].Angle = aB;
            data.Positions[_indexC].Center = cC;
            data.Positions[_indexC].Angle = aC;
            data.Positions[_indexD].Center = cD;
            data.Positions[_indexD].Angle = aD;

            // TODO_ERIN not implemented
            return linearError < PhysicsSettings.LinearSlop;
        }
    }
}
