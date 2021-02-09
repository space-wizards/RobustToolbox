using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    // TODO: FOR THIS LICENCE ALSO PUT BOX2D ON IT.
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
    public sealed class DistanceJoint : Joint, IEquatable<DistanceJoint>
    {
        // Sloth note:
        // Box2D is replacing rope with distance hence this is also a partial port of Box2D

         // Solver shared
        [NonSerialized] private float _bias;
        [NonSerialized] private float _gamma;
        [NonSerialized] private float _impulse;

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
        /// <param name="useWorldCoordinates">Set to true if you are using world coordinates as anchors.</param>
        public DistanceJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2 anchorA, Vector2 anchorB, bool useWorldCoordinates = false)
            : base(bodyA, bodyB)
        {
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
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalAnchorA { get; set; }

        /// <summary>
        /// The local anchor point relative to bodyB's origin.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalAnchorB { get; set; }

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
        [ViewVariables]
        public float Length { get; set; }

        /// <summary>
        /// The mass-spring-damper frequency in Hertz. A value of 0
        /// disables softness.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Frequency { get; set; }

        /// <summary>
        /// The damping ratio. 0 = no damping, 1 = critical damping.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
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

        internal override void InitVelocityConstraints(SolverData data)
        {
            _indexA = BodyA.IslandIndex;
            _indexB = BodyB.IslandIndex;
            _localCenterA = Vector2.Zero; // BodyA._sweep.LocalCenter;
            _localCenterB = Vector2.Zero; // BodyB._sweep.LocalCenter;
            _invMassA = BodyA.InvMass;
            _invMassB = BodyB.InvMass;
            _invIA = BodyA.InvI;
            _invIB = BodyB.InvI;

            Vector2 cA = data.Positions[_indexA];
            float aA = data.Angles[_indexA];
            Vector2 vA = data.LinearVelocities[_indexA];
            float wA = data.AngularVelocities[_indexA];

            Vector2 cB = data.Positions[_indexB];
            float aB = data.Angles[_indexB];
            Vector2 vB = data.LinearVelocities[_indexB];
            float wB = data.AngularVelocities[_indexB];

            Quaternion qA = new(aA), qB = new(aB);

            _rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
            _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
            _u = cB + _rB - cA - _rA;

            var configManager = IoCManager.Resolve<IConfigurationManager>();

            // Handle singularity.
            float length = _u.Length;
            if (length > configManager.GetCVar(CVars.LinearSlop))
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
                float omega = 2.0f * MathF.PI * Frequency;

                // Damping coefficient
                float d = 2.0f * _mass * DampingRatio * omega;

                // Spring stiffness
                float k = _mass * omega * omega;

                // magic formulas
                float h = data.FrameTime;
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

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (configManager.GetCVar(CVars.WarmStarting))
            {
                // Scale the impulse to support a variable time step.
                _impulse *= data.DtRatio;

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

            data.LinearVelocities[_indexA] = vA;
            data.AngularVelocities[_indexA] = wA;
            data.LinearVelocities[_indexB] = vB;
            data.AngularVelocities[_indexB] = wB;
        }

        internal override void SolveVelocityConstraints(SolverData data)
        {
            Vector2 vA = data.LinearVelocities[_indexA];
            float wA = data.AngularVelocities[_indexA];
            Vector2 vB = data.LinearVelocities[_indexB];
            float wB = data.AngularVelocities[_indexB];

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

            data.LinearVelocities[_indexA] = vA;
            data.AngularVelocities[_indexA] = wA;
            data.LinearVelocities[_indexB] = vB;
            data.AngularVelocities[_indexB] = wB;

        }

        internal override bool SolvePositionConstraints(SolverData data)
        {
            if (Frequency > 0.0f)
            {
                // There is no position correction for soft distance constraints.
                return true;
            }

            Vector2 cA = data.Positions[_indexA];
            float aA = data.Angles[_indexA];
            Vector2 cB = data.Positions[_indexB];
            float aB = data.Angles[_indexB];

            Quaternion qA = new(aA), qB = new(aB);

            Vector2 rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
            Vector2 rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
            Vector2 u = cB + rB - cA - rA;

            float length = u.Length;
            u = u.Normalized;
            float C = length - Length;

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var maxLinearCorrection = configManager.GetCVar(CVars.MaxLinearCorrection);
            var linearSlop = configManager.GetCVar(CVars.LinearSlop);

            C = Math.Clamp(C, -maxLinearCorrection, maxLinearCorrection);

            float impulse = -_mass * C;
            Vector2 P = u * impulse;

            cA -= P * _invMassA;
            aA -= _invIA * Vector2.Cross(rA, P);
            cB += P * _invMassB;
            aB += _invIB * Vector2.Cross(rB, P);

            data.Positions[_indexA] = cA;
            data.Angles[_indexA] = aA;
            data.Positions[_indexB] = cB;
            data.Angles[_indexB] = aB;

            return Math.Abs(C) < linearSlop;
        }

        public bool Equals(DistanceJoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return LocalAnchorA.EqualsApprox(other.LocalAnchorA) &&
                   LocalAnchorB.EqualsApprox(other.LocalAnchorB) &&
                   MathHelper.CloseTo(Length, other.Length) &&
                   MathHelper.CloseTo(Frequency, other.Frequency) &&
                   MathHelper.CloseTo(DampingRatio, other.DampingRatio);
        }
    }
}
