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

/*
 * Farseer DistanceJoint but with some recent Box2D additions
 */

using System;
using System.Numerics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints;

[Serializable, NetSerializable]
internal sealed class DistanceJointState : JointState
{
    public float Length { get; internal set; }
    public float MinLength { get; internal set; }
    public float MaxLength { get; internal set; }
    public float Stiffness { get; internal set; }
    public float Damping { get; internal set; }

    public override Joint GetJoint(IEntityManager entManager, EntityUid owner)
    {
        return new DistanceJoint(this, entManager, owner);
    }
}

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
public sealed partial class DistanceJoint : Joint, IEquatable<DistanceJoint>
{
    // Sloth note:
    // Box2D is replacing rope with distance hence this is also a partial port of Box2D

    // Solver shared
    private float _bias;
    private float _gamma;
    private float _impulse;
    private float _lowerImpulse;
    private float _upperImpulse;

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
    private float _currentLength;
    private float _softMass;

    public override JointType JointType => JointType.Distance;

    public DistanceJoint() {}

    /// <summary>
    /// This requires defining an
    /// anchor point on both bodies and the non-zero length of the
    /// distance joint. If you don't supply a length, the local anchor points
    /// is used so that the initial configuration can violate the constraint
    /// slightly. This helps when saving and loading a game.
    /// Warning Do not use a zero or short length.
    /// </summary>
    public DistanceJoint(EntityUid uidA, EntityUid uidB, Vector2 anchorA, Vector2 anchorB, float length)
        : base(uidA, uidB)
    {
        Length = MathF.Max(PhysicsConstants.LinearSlop, length);
        _minLength = _length;
        _maxLength = _length;
        LocalAnchorA = anchorA;
        LocalAnchorB = anchorB;
    }

    internal DistanceJoint(DistanceJointState state, IEntityManager entManager, EntityUid owner)
        : base(state, entManager, owner)
    {
        _damping = state.Damping;
        _length = state.Length;
        _maxLength = state.MaxLength;
        _minLength = state.MinLength;
        _stiffness = state.Stiffness;
    }

    /// <summary>
    /// The natural length between the anchor points.
    /// Manipulating the length can lead to non-physical behavior when the frequency is zero.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("length")]
    public float Length
    {
        get => _length;
        set
        {
            if (MathHelper.CloseTo(value, _length)) return;

            _impulse = 0.0f;
            _length = MathF.Max(value, PhysicsConstants.LinearSlop);
            Dirty();
        }
    }

    private float _length;

    /// <summary>
    ///     The upper limit allowed between the 2 bodies.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength
    {
        get => _maxLength;
        set
        {
            if (MathHelper.CloseTo(value, _maxLength)) return;

            _upperImpulse = 0.0f;
            _maxLength = MathF.Max(value, _minLength);
            Dirty();
        }
    }

    private float _maxLength;

    /// <summary>
    ///     The lower limit allowed between the 2 bodies.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("minLength")]
    public float MinLength
    {
        get => _minLength;
        set
        {
            if (MathHelper.CloseTo(value, _minLength)) return;

            _lowerImpulse = 0.0f;
            _minLength = Math.Clamp(value, PhysicsConstants.LinearSlop, MaxLength);
            Dirty();
        }
    }

    private float _minLength;

    /// <summary>
    /// The linear stiffness in N/m.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stiffness")]
    public float Stiffness
    {
        get => _stiffness;
        set
        {
            if (MathHelper.CloseTo(_stiffness, value)) return;

            _stiffness = value;
            Dirty();
        }
    }

    private float _stiffness;

    /// <summary>
    /// The linear damping in N*s/m.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("damping")]
    public float Damping
    {
        get => _damping;
        set
        {
            if (MathHelper.CloseTo(_damping, value)) return;

            _damping = value;
            Dirty();
        }
    }

    private float _damping;

    /// <summary>
    /// Get the reaction force given the inverse time step. Unit is N.
    /// </summary>
    /// <param name="invDt"></param>
    /// <returns></returns>
    public override Vector2 GetReactionForce(float invDt)
    {
        Vector2 F = _u * invDt * (_impulse + _lowerImpulse - _upperImpulse);
        return F;
    }

    public override JointState GetState(IEntityManager entManager)
    {
        var distanceState = new DistanceJointState
        {
            Damping = _damping,
            Length = _length,
            MinLength = _minLength,
            MaxLength = _maxLength,
            Stiffness = _stiffness,
            LocalAnchorA = LocalAnchorA,
            LocalAnchorB = LocalAnchorB
        };

        base.GetState(distanceState, entManager);
        return distanceState;
    }

    internal override void ApplyState(JointState state)
    {
        base.ApplyState(state);
        if (state is not DistanceJointState distanceState) return;

        _damping = distanceState.Damping;
        _length = distanceState.Length;
        _minLength = distanceState.MinLength;
        _maxLength = distanceState.MaxLength;
        _stiffness = distanceState.Stiffness;
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

        var cA = positions[_indexA];
        float aA = angles[_indexA];
        var vA = linearVelocities[offset + _indexA];
        float wA = angularVelocities[offset + _indexA];

        var cB = positions[_indexB];
        float aB = angles[_indexB];
        var vB = linearVelocities[offset + _indexB];
        float wB = angularVelocities[offset + _indexB];

        Quaternion2D qA = new(aA), qB = new(aB);

        _rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
        _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
        _u = cB + _rB - cA - _rA;

        // Handle singularity.
        _currentLength = _u.Length();
        if (_currentLength > PhysicsConstants.LinearSlop)
        {
            _u *= 1.0f / _currentLength;
        }
        else
        {
            _u = Vector2.Zero;
            _mass = 0.0f;
            _impulse = 0.0f;
            _lowerImpulse = 0.0f;
            _upperImpulse = 0.0f;
        }

        float crAu = Vector2Helpers.Cross(_rA, _u);
        float crBu = Vector2Helpers.Cross(_rB, _u);
        float invMass = _invMassA + _invIA * crAu * crAu + _invMassB + _invIB * crBu * crBu;
        _mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;

        if (Stiffness > 0.0f && _minLength < _maxLength)
        {
            // soft
            float C = _currentLength - _length;

            float d = Damping;
            float k = Stiffness;

            // magic formulas
            float h = data.FrameTime;

            // gamma = 1 / (h * (d + h * k))
            // the extra factor of h in the denominator is since the lambda is an impulse, not a force
            _gamma = h * (d + h * k);
            _gamma = _gamma != 0.0f ? 1.0f / _gamma : 0.0f;
            _bias = C * h * k * _gamma;

            invMass += _gamma;
            _softMass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
        }
        else
        {
            // rigid
            _gamma = 0.0f;
            _bias = 0.0f;
            _softMass = _mass;
        }

        if (data.WarmStarting)
        {
            // Scale the impulse to support a variable time step.
            _impulse *= data.DtRatio;
            _lowerImpulse *= data.DtRatio;
            _upperImpulse *= data.DtRatio;

            var P = _u * (_impulse + _lowerImpulse - _upperImpulse);
            vA -= P * _invMassA;
            wA -= _invIA * Vector2Helpers.Cross(_rA, P);
            vB += P * _invMassB;
            wB += _invIB * Vector2Helpers.Cross(_rB, P);
        }
        else
        {
            _impulse = 0.0f;
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

        if (_minLength < _maxLength)
        {
            if (Stiffness > 0.0f)
            {
                // Cdot = dot(u, v + cross(w, r))
                var vpA = vA + Vector2Helpers.Cross(wA, _rA);
                var vpB = vB + Vector2Helpers.Cross(wB, _rB);
                float Cdot = Vector2.Dot(_u, vpB - vpA);

                float impulse = -_softMass * (Cdot + _bias + _gamma * _impulse);
                _impulse += impulse;

                // TODO: Ability to make this one-sided.
                var P = _u * impulse;
                vA -= P * _invMassA;
                wA -= _invIA * Vector2Helpers.Cross(_rA, P);
                vB += P * _invMassB;
                wB += _invIB * Vector2Helpers.Cross(_rB, P);
            }

            // lower
            {
                float C = _currentLength - _minLength;
                float bias = MathF.Max(0.0f, C) * data.InvDt;

                var vpA = vA + Vector2Helpers.Cross(wA, _rA);
                var vpB = vB + Vector2Helpers.Cross(wB, _rB);
                float Cdot = Vector2.Dot(_u, vpB - vpA);

                float impulse = -_mass * (Cdot + bias);
                float oldImpulse = _lowerImpulse;
                _lowerImpulse = MathF.Max(0.0f, _lowerImpulse + impulse);
                impulse = _lowerImpulse - oldImpulse;
                var P = _u * impulse;

                vA -= P * _invMassA;
                wA -= _invIA * Vector2Helpers.Cross(_rA, P);
                vB += P * _invMassB;
                wB += _invIB * Vector2Helpers.Cross(_rB, P);
            }

            // upper
            {
                float C = _maxLength - _currentLength;
                float bias = MathF.Max(0.0f, C) * data.InvDt;

                var vpA = vA + Vector2Helpers.Cross(wA, _rA);
                var vpB = vB + Vector2Helpers.Cross(wB, _rB);
                float Cdot = Vector2.Dot(_u, vpA - vpB);

                float impulse = -_mass * (Cdot + bias);
                float oldImpulse = _upperImpulse;
                _upperImpulse = MathF.Max(0.0f, _upperImpulse + impulse);
                impulse = _upperImpulse - oldImpulse;
                var P = _u * -impulse;

                vA -= P * _invMassA;
                wA -= _invIA * Vector2Helpers.Cross(_rA, P);
                vB += P * _invMassB;
                wB += _invIB * Vector2Helpers.Cross(_rB, P);
            }
        }
        else
        {
            // Equal limits

            // Cdot = dot(u, v + cross(w, r))
            var vpA = vA + Vector2Helpers.Cross(wA, _rA);
            var vpB = vB + Vector2Helpers.Cross(wB, _rB);
            float Cdot = Vector2.Dot(_u, vpB - vpA);

            float impulse = -_mass * Cdot;
            _impulse += impulse;

            var P = _u * impulse;
            vA -= P * _invMassA;
            wA -= _invIA * Vector2Helpers.Cross(_rA, P);
            vB += P * _invMassB;
            wB += _invIB * Vector2Helpers.Cross(_rB, P);
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

        var rA = Transform.Mul(qA, LocalAnchorA - _localCenterA);
        var rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);
        var u = cB + rB - cA - rA;

        float length = u.Length();
        u = u.Normalized();
        float C;
        if (MathHelper.CloseTo(_minLength, _maxLength))
        {
            C = length - _minLength;
        }
        else if (length < _minLength)
        {
            C = length - _minLength;
        }
        else if (_maxLength < length)
        {
            C = length - _maxLength;
        }
        else
        {
            return true;
        }

        float impulse = -_mass * C;
        var P = u * impulse;

        cA -= P * _invMassA;
        aA -= _invIA * Vector2Helpers.Cross(rA, P);
        cB += P * _invMassB;
        aB += _invIB * Vector2Helpers.Cross(rB, P);

        positions[_indexA] = cA;
        angles[_indexA] = aA;
        positions[_indexB] = cB;
        angles[_indexB] = aB;

        return MathF.Abs(C) < PhysicsConstants.LinearSlop;
    }

    public override Joint Clone(EntityUid uidA, EntityUid uidB)
    {
        var distance = new DistanceJoint(uidA, uidB, LocalAnchorA, LocalAnchorB, Length)
        {
            Enabled = Enabled,
            MinLength = MinLength,
            MaxLength = MaxLength,
            Stiffness = Stiffness,
            Damping = Damping,
            _lowerImpulse = _lowerImpulse,
            _upperImpulse = _upperImpulse,
            _impulse = _impulse,
            Breakpoint = Breakpoint
        };
        return distance;
    }

    public override void CopyTo(Joint original)
    {
        if (original is not DistanceJoint distance)
            return;

        distance.Enabled = Enabled;
        distance.MinLength = MinLength;
        distance.MaxLength = MaxLength;
        distance.Length = Length;
        distance.Stiffness = Stiffness;
        distance.Damping = Damping;
        distance._lowerImpulse = _lowerImpulse;
        distance._upperImpulse = _upperImpulse;
        distance._impulse = _impulse;
        distance.Breakpoint = Breakpoint;
    }

    public bool Equals(DistanceJoint? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!base.Equals(other)) return false;

        return MathHelper.CloseTo(_length, other._length) &&
               MathHelper.CloseTo(_minLength, other._minLength) &&
               MathHelper.CloseTo(_maxLength, other._maxLength) &&
               MathHelper.CloseTo(_stiffness, other._stiffness) &&
               MathHelper.CloseTo(_damping, other._damping);
    }
}
