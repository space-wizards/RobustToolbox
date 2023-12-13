using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Robust.Shared.Physics.Dynamics.Joints;

[Serializable, NetSerializable]
internal sealed class WeldJointState : JointState
{
    public float Stiffness { get; internal set; }
    public float Damping { get; internal set; }
    public float Bias { get; internal set; }

    public override Joint GetJoint(IEntityManager entManager, EntityUid owner)
    {
        return new WeldJoint(this, entManager, owner);
    }
}

public sealed partial class WeldJoint : Joint, IEquatable<WeldJoint>
{
    // Shared
    private float _gamma;
    private Vector3 _impulse;

    // Temporary
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
    private Matrix33 _mass;

    // Settable

    [DataField("stiffness")]
    public float Stiffness;

    [DataField("damping")]
    public float Damping;

    [DataField("bias")]
    public float Bias;

    /// <summary>
    /// The bodyB angle minus bodyA angle in the reference state (radians).
    /// </summary>
    [DataField("referenceAngle")]
    public float ReferenceAngle;

    /// <summary>
    /// Used for Serv3 reasons
    /// </summary>
    public WeldJoint() {}

    internal WeldJoint(EntityUid bodyA, EntityUid bodyB, Vector2 anchorA, Vector2 anchorB, float referenceAngle) : base(bodyA, bodyB)
    {
        LocalAnchorA = anchorA;
        LocalAnchorB = anchorB;
        ReferenceAngle = referenceAngle;
    }

    internal WeldJoint(EntityUid bodyAUid, EntityUid bodyBUid) : base(bodyAUid, bodyBUid) {}

    internal WeldJoint(WeldJointState state, IEntityManager entManager, EntityUid owner)
        : base(state, entManager, owner)
    {
        Stiffness = state.Stiffness;
        Damping = state.Damping;
        Bias = state.Bias;
    }

    public override JointType JointType => JointType.Weld;
    public override JointState GetState(IEntityManager entManager)
    {
        var weldJointState = new WeldJointState();

        base.GetState(weldJointState, entManager);
        return weldJointState;
    }

    public override Vector2 GetReactionForce(float invDt)
    {
        var P = new Vector2(_impulse.X, _impulse.Y);
        return P * invDt;
    }

    public override float GetReactionTorque(float invDt)
    {
        return invDt * _impulse.Z;
    }

    internal override void InitVelocityConstraints(
        in SolverData data,
        in SharedPhysicsSystem.IslandData island,
        PhysicsComponent bodyA,
        PhysicsComponent bodyB,
        Vector2[] positions,
        float[] angles,
        Vector2[] linearVelocities,
        float[] angularVelocities)
    {
        var offset = island.Offset;
        _indexA = bodyA.IslandIndex[island.Index];
        _indexB = bodyB.IslandIndex[island.Index];
        _localCenterA = bodyA.LocalCenter;
        _localCenterB = bodyB.LocalCenter;
        _invMassA = bodyA.InvMass;
        _invMassB = bodyB.InvMass;
        _invIA = bodyA.InvI;
        _invIB = bodyB.InvI;

        float aA = angles[_indexA];
        var vA = linearVelocities[offset + _indexA];
        float wA = angularVelocities[offset + _indexA];

        float aB = angles[_indexB];
        var vB = linearVelocities[offset + _indexB];
        float wB = angularVelocities[offset + _indexB];

        Quaternion2D qA = new(aA), qB = new(aB);

        _rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
        _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

        // J = [-I -r1_skew I r2_skew]
        //     [ 0       -1 0       1]
        // r_skew = [-ry; rx]

        // Matlab
        // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
        //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
        //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]

        float mA = _invMassA, mB = _invMassB;
        float iA = _invIA, iB = _invIB;

        Matrix33 K;
        K.EX.X = mA + mB + _rA.Y * _rA.Y * iA + _rB.Y * _rB.Y * iB;
        K.EY.X = -_rA.Y * _rA.X * iA - _rB.Y * _rB.X * iB;
        K.EZ.X = -_rA.Y * iA - _rB.Y * iB;
        K.EX.Y = K.EY.X;
        K.EY.Y = mA + mB + _rA.X * _rA.X * iA + _rB.X * _rB.X * iB;
        K.EZ.Y = _rA.X * iA + _rB.X * iB;
        K.EX.Z = K.EZ.X;
        K.EY.Z = K.EZ.Y;
        K.EZ.Z = iA + iB;

        if (Stiffness > 0.0f)
        {
            K.GetInverse22(ref _mass);

            float invM = iA + iB;

            float C = aB - aA - ReferenceAngle;

            // Damping coefficient
            float d = Damping;

            // Spring stiffness
            float k = Stiffness;

            // magic formulas
            float h = data.FrameTime;
            _gamma = h * (d + h * k);
            _gamma = _gamma != 0.0f ? 1.0f / _gamma : 0.0f;
            Bias = C * h * k * _gamma;

            invM += _gamma;
            _mass.EZ.Z = invM != 0.0f ? 1.0f / invM : 0.0f;
        }
        else if (K.EZ.Z == 0.0f)
        {
            K.GetInverse22(ref _mass);
            _gamma = 0.0f;
            Bias = 0.0f;
        }
        else
        {
            K.GetSymInverse33(ref _mass);
            _gamma = 0.0f;
            Bias = 0.0f;
        }

        if (data.WarmStarting)
        {
            // Scale impulses to support a variable time step.
            _impulse *= data.DtRatio;

            var P = new Vector2(_impulse.X, _impulse.Y);

            vA -= P * mA;
            wA -= iA * (Vector2Helpers.Cross(_rA, P) + _impulse.Z);

            vB += P * mB;
            wB += iB * (Vector2Helpers.Cross(_rB, P) + _impulse.Z);
        }
        else
        {
            _impulse = Vector3.Zero;
        }

        linearVelocities[offset + _indexA] = vA;
        angularVelocities[offset + _indexA] = wA;
        linearVelocities[offset + _indexB] = vB;
        angularVelocities[offset + _indexB] = wB;
    }

    internal override void SolveVelocityConstraints(
        in SolverData data,
        in SharedPhysicsSystem.IslandData island,
        Vector2[] linearVelocities,
        float[] angularVelocities)
    {
        var offset = island.Offset;
        var vA = linearVelocities[offset + _indexA];
        float wA = angularVelocities[offset + _indexA];
        var vB = linearVelocities[offset + _indexB];
        float wB = angularVelocities[offset + _indexB];

        float mA = _invMassA, mB = _invMassB;
        float iA = _invIA, iB = _invIB;

        if (Stiffness > 0.0f)
        {
            float Cdot2 = wB - wA;

            float impulse2 = -_mass.EZ.Z * (Cdot2 + Bias + _gamma * _impulse.Z);
            _impulse.Z += impulse2;

            wA -= iA * impulse2;
            wB += iB * impulse2;

            var Cdot1 = vB + Vector2Helpers.Cross(wB, _rB) - vA - Vector2Helpers.Cross(wA, _rA);

            var impulse1 = -_mass.Mul22(Cdot1);
            _impulse.X += impulse1.X;
            _impulse.Y += impulse1.Y;

            var P = impulse1;

            vA -= P * mA;
            wA -= iA * Vector2Helpers.Cross(_rA, P);

            vB += P * mB;
            wB += iB * Vector2Helpers.Cross(_rB, P);
        }
        else
        {
            var Cdot1 = vB + Vector2Helpers.Cross(wB, _rB) - vA - Vector2Helpers.Cross(wA, _rA);
            float Cdot2 = wB - wA;
            var Cdot = new Vector3(Cdot1.X, Cdot1.Y, Cdot2);

            var impulse = -_mass.Mul(Cdot);
            _impulse += impulse;

            var P = new Vector2(impulse.X, impulse.Y);

            vA -= P * mA;
            wA -= iA * (Vector2Helpers.Cross(_rA, P) + impulse.Z);

            vB += P * mB;
            wB += iB * (Vector2Helpers.Cross(_rB, P) + impulse.Z);
        }

        linearVelocities[offset + _indexA] = vA;
        angularVelocities[offset + _indexA] = wA;
        linearVelocities[offset + _indexB] = vB;
        angularVelocities[offset + _indexB] = wB;
    }

    internal override bool SolvePositionConstraints(
        in SolverData data,
        Vector2[] positions,
        float[] angles)
    {
        var cA = positions[_indexA];
        float aA = angles[_indexA];
        var cB = positions[_indexB];
        float aB = angles[_indexB];

        Quaternion2D qA = new(aA), qB = new(aB);

        float mA = _invMassA, mB = _invMassB;
        float iA = _invIA, iB = _invIB;

        var rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
        var rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

        float positionError, angularError;

        Matrix33 K;
        K.EX.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
        K.EY.X = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
        K.EZ.X = -rA.Y * iA - rB.Y * iB;
        K.EX.Y = K.EY.X;
        K.EY.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
        K.EZ.Y = rA.X * iA + rB.X * iB;
        K.EX.Z = K.EZ.X;
        K.EY.Z = K.EZ.Y;
        K.EZ.Z = iA + iB;

        if (Stiffness > 0.0f)
        {
            var C1 =  cB + rB - cA - rA;

            positionError = C1.Length();
            angularError = 0.0f;

            var P = -K.Solve22(C1);

            cA -= P * mA;
            aA -= iA * Vector2Helpers.Cross(rA, P);

            cB += P * mB;
            aB += iB * Vector2Helpers.Cross(rB, P);
        }
        else
        {
            var C1 =  cB + rB - cA - rA;
            float C2 = aB - aA - ReferenceAngle;

            positionError = C1.Length();
            angularError = Math.Abs(C2);

            Vector3 C = new(C1.X, C1.Y, C2);

            Vector3 impulse;
            if (K.EZ.Z > 0.0f)
            {
                impulse = -K.Solve33(C);
            }
            else
            {
                var impulse2 = -K.Solve22(C1);
                impulse = new Vector3(impulse2.X, impulse2.Y, 0.0f);
            }

            var P = new Vector2(impulse.X, impulse.Y);

            cA -= P * mA;
            aA -= iA * (Vector2Helpers.Cross(rA, P) + impulse.Z);

            cB += P * mB;
            aB += iB * (Vector2Helpers.Cross(rB, P) + impulse.Z);
        }

        positions[_indexA] = cA;
        angles[_indexA]= aA;
        positions[_indexB] = cB;
        angles[_indexB] = aB;

        return positionError <= PhysicsConstants.LinearSlop && angularError <= PhysicsConstants.AngularSlop;
    }

    public override Joint Clone(EntityUid uidA, EntityUid uidB)
    {
        var weld = new WeldJoint(uidA, uidB, LocalAnchorA, LocalAnchorB, ReferenceAngle)
        {
            Enabled = Enabled,
            Bias = Bias,
            Damping = Damping,
            Stiffness = Stiffness,
            _impulse = _impulse,
            Breakpoint = Breakpoint
        };
        return weld;
    }

    public override void CopyTo(Joint original)
    {
        if (original is not WeldJoint weld)
            return;

        weld.Enabled = Enabled;
        weld.Bias = Bias;
        weld.Damping = Damping;
        weld.Stiffness = Stiffness;
        weld._impulse = _impulse;
        weld.Breakpoint = Breakpoint;
    }

    public bool Equals(WeldJoint? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!base.Equals(other)) return false;

        return Stiffness.Equals(other.Stiffness) &&
               Damping.Equals(other.Damping) &&
               Bias.Equals(other.Bias);
    }
}
