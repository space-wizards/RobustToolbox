// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Dynamics.Joints;

[Serializable, NetSerializable]
internal sealed class RevoluteJointState : JointState
{
    public bool EnableLimit { get; internal set; }
    public bool EnableMotor { get; internal set; }
    public float ReferenceAngle { get; internal set; }
    public float LowerAngle { get; internal set; }
    public float UpperAngle { get; internal set; }
    public float MotorSpeed { get; internal set; }
    public float MaxMotorTorque { get; internal set; }

    public override Joint GetJoint(IEntityManager entManager, EntityUid owner)
    {
        return new RevoluteJoint(this, entManager, owner);
    }
}

public sealed partial class RevoluteJoint : Joint, IEquatable<RevoluteJoint>
{
    // Temporary
    private Vector2 _impulse;
    private int _indexA;
    private int _indexB;
    private Vector2 _localCenterA;
    private Vector2 _localCenterB;
    private float _invMassA;
    private float _invMassB;
    private float _invIA;
    private float _invIB;
    private Vector2 _rA;
    private Vector2 _rB;
    private Matrix22 _K;
    private float _axialMass;
    private float _angle;
    private float _motorImpulse;
    private float _lowerImpulse;
    private float _upperImpulse;

    // Settable
    [DataField("enableLimit")]
    public bool EnableLimit;

    /// <summary>
    /// A flag to enable the joint motor.
    /// </summary>
    [DataField("enableMotor")]
    public bool EnableMotor;

    /// <summary>
    /// The bodyB angle minus bodyA angle in the reference state (radians).
    /// </summary>
    [DataField("referenceAngle")]
    public float ReferenceAngle;

    /// <summary>
    /// The lower angle for the joint limit (radians).
    /// </summary>
    [DataField("lowerAngle")]
    public float LowerAngle;

    /// <summary>
    /// The upper angle for the joint limit (radians).
    /// </summary>
    [DataField("upperAngle")]
    public float UpperAngle;

    /// <summary>
    /// The desired motor speed. Usually in radians per second.
    /// </summary>
    [DataField("motorSpeed")]
    public float MotorSpeed;

    /// <summary>
    /// The maximum motor torque used to achieve the desired motor speed.
    /// Usually in N-m.
    /// </summary>
    [DataField("maxMotorTorque")]
    public float MaxMotorTorque;

    public RevoluteJoint() {}

    public RevoluteJoint(EntityUid uidA, EntityUid uidB, Vector2 anchorA, Vector2 anchorB, float referenceAngle) : base(uidA, uidB)
    {
        LocalAnchorA = anchorA;
        LocalAnchorB = anchorB;
        ReferenceAngle = referenceAngle;
    }

    public RevoluteJoint(EntityUid bodyAUid, EntityUid bodyBUid) : base(bodyAUid, bodyBUid) {}

    internal RevoluteJoint(RevoluteJointState state, IEntityManager entManager, EntityUid owner)
        : base(state, entManager, owner)
    {
        EnableLimit = state.EnableLimit;
        EnableMotor = state.EnableMotor;
        ReferenceAngle = state.ReferenceAngle;
        LowerAngle = state.LowerAngle;
        UpperAngle = state.UpperAngle;
        MotorSpeed = state.MotorSpeed;
        MaxMotorTorque = state.MaxMotorTorque;
    }

    public override JointType JointType => JointType.Revolute;

    public override JointState GetState(IEntityManager entManager)
    {
        var revoluteState = new RevoluteJointState();

        base.GetState(revoluteState, entManager);
        return revoluteState;
    }

    internal override void ApplyState(JointState state)
    {
        base.ApplyState(state);
        if (state is not RevoluteJointState revoluteState) return;

        EnableLimit = revoluteState.EnableLimit;
        EnableMotor = revoluteState.EnableMotor;
        LowerAngle = revoluteState.LowerAngle;
        MotorSpeed = revoluteState.MotorSpeed;
        ReferenceAngle = revoluteState.ReferenceAngle;
        UpperAngle = revoluteState.UpperAngle;
        MaxMotorTorque = revoluteState.MaxMotorTorque;
    }

    public override Vector2 GetReactionForce(float invDt)
    {
        var P = new Vector2(_impulse.X, _impulse.Y);
        return P * invDt;
    }

    public override float GetReactionTorque(float invDt)
    {
        return invDt * (_motorImpulse + _lowerImpulse - _upperImpulse);
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
        // r_skew = [-ry; rx]

        // Matlab
        // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x]
        //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB]

        float mA = _invMassA, mB = _invMassB;
        float iA = _invIA, iB = _invIB;

        _K.EX.X = mA + mB + _rA.Y * _rA.Y * iA + _rB.Y * _rB.Y * iB;
        _K.EY.X = -_rA.Y * _rA.X * iA - _rB.Y * _rB.X * iB;
        _K.EX.Y = _K.EY.X;
        _K.EY.Y = mA + mB + _rA.X * _rA.X * iA + _rB.X * _rB.X * iB;

        _axialMass = iA + iB;
        bool fixedRotation;
        if (_axialMass > 0.0f)
        {
            _axialMass = 1.0f / _axialMass;
            fixedRotation = false;
        }
        else
        {
            fixedRotation = true;
        }

        _angle = aB - aA - ReferenceAngle;
        if (EnableLimit == false || fixedRotation)
        {
            _lowerImpulse = 0.0f;
            _upperImpulse = 0.0f;
        }

        if (EnableMotor == false || fixedRotation)
        {
            _motorImpulse = 0.0f;
        }

        if (data.WarmStarting)
        {
            // Scale impulses to support a variable time step.
            _impulse *= data.DtRatio;
            _motorImpulse *= data.DtRatio;
            _lowerImpulse *= data.DtRatio;
            _upperImpulse *= data.DtRatio;

            float axialImpulse = _motorImpulse + _lowerImpulse - _upperImpulse;
            var P = new Vector2(_impulse.X, _impulse.Y);

            vA -= P * mA;
            wA -= iA * (Vector2Helpers.Cross(_rA, P) + axialImpulse);

            vB += P * mB;
            wB += iB * (Vector2Helpers.Cross(_rB, P) + axialImpulse);
        }
        else
        {
            _impulse = Vector2.Zero;
            _motorImpulse = 0.0f;
            _lowerImpulse = 0.0f;
            _upperImpulse = 0.0f;
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

        bool fixedRotation = (iA + iB == 0.0f);

        // Solve motor constraint.
        if (EnableMotor && !fixedRotation)
        {
            float Cdot = wB - wA - MotorSpeed;
            float impulse = -_axialMass * Cdot;
            float oldImpulse = _motorImpulse;
            float maxImpulse = data.FrameTime * MaxMotorTorque;
            _motorImpulse = Math.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
            impulse = _motorImpulse - oldImpulse;

            wA -= iA * impulse;
            wB += iB * impulse;
        }

        if (EnableLimit && fixedRotation == false)
        {
            // Lower limit
            {
                float C = _angle - LowerAngle;
                float Cdot = wB - wA;
                float impulse = -_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.InvDt);
                float oldImpulse = _lowerImpulse;
                _lowerImpulse = MathF.Max(_lowerImpulse + impulse, 0.0f);
                impulse = _lowerImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Upper limit
            // Note: signs are flipped to keep C positive when the constraint is satisfied.
            // This also keeps the impulse positive when the limit is active.
            {
                float C = UpperAngle - _angle;
                float Cdot = wA - wB;
                float impulse = -_axialMass * (Cdot + MathF.Max(C, 0.0f) * data.InvDt);
                float oldImpulse = _upperImpulse;
                _upperImpulse = MathF.Max(_upperImpulse + impulse, 0.0f);
                impulse = _upperImpulse - oldImpulse;

                wA += iA * impulse;
                wB -= iB * impulse;
            }
        }

        // Solve point-to-point constraint
        {
            var Cdot = vB + Vector2Helpers.Cross(wB, _rB) - vA - Vector2Helpers.Cross(wA, _rA);
            var impulse = _K.Solve(-Cdot);

            _impulse.X += impulse.X;
            _impulse.Y += impulse.Y;

            vA -= impulse * mA;
            wA -= iA * Vector2Helpers.Cross(_rA, impulse);

            vB += impulse * mB;
            wB += iB * Vector2Helpers.Cross(_rB, impulse);
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

        float angularError = 0.0f;
        float positionError = 0.0f;

        bool fixedRotation = (_invIA + _invIB == 0.0f);

        // Solve angular limit constraint
        if (EnableLimit && fixedRotation == false)
        {
            float angle = aB - aA - ReferenceAngle;
            float C = 0.0f;

            if (Math.Abs(UpperAngle - LowerAngle) < 2.0f * PhysicsConstants.AngularSlop)
            {
                // Prevent large angular corrections
                C = Math.Clamp(angle - LowerAngle, -data.MaxAngularCorrection, data.MaxAngularCorrection);
            }
            else if (angle <= LowerAngle)
            {
                // Prevent large angular corrections and allow some slop.
                C = Math.Clamp(angle - LowerAngle + PhysicsConstants.AngularSlop, -data.MaxAngularCorrection, 0.0f);
            }
            else if (angle >= UpperAngle)
            {
                // Prevent large angular corrections and allow some slop.
                C = Math.Clamp(angle - UpperAngle - PhysicsConstants.AngularSlop, 0.0f, data.MaxAngularCorrection);
            }

            float limitImpulse = -_axialMass * C;
            aA -= _invIA * limitImpulse;
            aB += _invIB * limitImpulse;
            angularError = Math.Abs(C);
        }

        // Solve point-to-point constraint.
        {
            qA.Set(aA);
            qB.Set(aB);
            var rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
            var rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

            var C = cB + rB - cA - rA;
            positionError = C.Length();

            float mA = _invMassA, mB = _invMassB;
            float iA = _invIA, iB = _invIB;

            var K = new Matrix22(
                mA + mB + iA * rA.Y * rA.Y + iB * rB.Y * rB.Y,
                -iA * rA.X * rA.Y - iB * rB.X * rB.Y,
                0f,
                mA + mB + iA * rA.X * rA.X + iB * rB.X * rB.X);

            K.EY.X = K.EX.Y;

            var impulse = -K.Solve(C);

            cA -= impulse * mA;
            aA -= iA * Vector2Helpers.Cross(rA, impulse);

            cB += impulse * mB;
            aB += iB * Vector2Helpers.Cross(rB, impulse);
        }

        positions[_indexA] = cA;
        angles[_indexA] = aA;
        positions[_indexB] = cB;
        angles[_indexB] = aB;

        return positionError <= PhysicsConstants.LinearSlop && angularError <= PhysicsConstants.AngularSlop;
    }

    public bool Equals(RevoluteJoint? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!base.Equals(other)) return false;

        return EnableLimit == other.EnableLimit &&
               EnableMotor == other.EnableMotor &&
               MathHelper.CloseTo(ReferenceAngle, other.ReferenceAngle) &&
               MathHelper.CloseTo(LowerAngle, other.LowerAngle) &&
               MathHelper.CloseTo(UpperAngle, other.UpperAngle) &&
               MathHelper.CloseTo(MotorSpeed, other.MotorSpeed) &&
               MathHelper.CloseTo(MaxMotorTorque, other.MaxMotorTorque);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != GetType()) return false;
        return Equals((RevoluteJoint) obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(BodyAUid);
        hashCode.Add(BodyBUid);
        return hashCode.ToHashCode();
    }

    public override Joint Clone(EntityUid uidA, EntityUid uidB)
    {
        var revolute = new RevoluteJoint(uidA, uidB, LocalAnchorA, LocalAnchorB, ReferenceAngle)
        {
            Enabled = Enabled,
            EnableLimit = EnableLimit,
            EnableMotor = EnableMotor,
            LowerAngle = LowerAngle,
            UpperAngle = UpperAngle,
            MaxMotorTorque = MaxMotorTorque,
            MotorSpeed = MotorSpeed,
            _impulse = _impulse,
            _upperImpulse = _upperImpulse,
            _lowerImpulse = _lowerImpulse,
            _motorImpulse = _motorImpulse,
            Breakpoint = Breakpoint
        };

        return revolute;
    }

    public override void CopyTo(Joint original)
    {
        if (original is not RevoluteJoint revolute)
            return;

        revolute.Enabled = Enabled;
        revolute.EnableLimit = EnableLimit;
        revolute.EnableMotor = EnableMotor;
        revolute.LowerAngle = LowerAngle;
        revolute.UpperAngle = UpperAngle;
        revolute.MaxMotorTorque = MaxMotorTorque;
        revolute.MotorSpeed = MotorSpeed;
        revolute._impulse = _impulse;
        revolute._upperImpulse = _upperImpulse;
        revolute._lowerImpulse = _lowerImpulse;
        revolute._motorImpulse = _motorImpulse;
        revolute.Breakpoint = Breakpoint;
    }
}
