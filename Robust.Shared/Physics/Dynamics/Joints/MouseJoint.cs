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
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints;

[Serializable, NetSerializable]
internal sealed class MouseJointState : JointState
{
    public float MaxForce { get; internal set; }
    public float Stiffness { get; internal set; }
    public float Damping { get; internal set; }

    public override Joint GetJoint()
    {
        return new MouseJoint(this);
    }
}


public sealed class MouseJoint : Joint, IEquatable<MouseJoint>
{
    public override JointType JointType => JointType.Mouse;

    /// <summary>
    /// The maximum constraint force that can be exerted
    /// to move the candidate body. Usually you will express
    /// as some multiple of the weight (multiplier * mass * gravity).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxForce")]
    public float MaxForce
    {
        get => _maxForce;
        set
        {
            if (MathHelper.CloseTo(_maxForce, value)) return;

            _maxForce = value;
            Dirty();
        }
    }

    private float _maxForce;

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
    /// The initial world target point. This is assumed
    /// to coincide with the body anchor initially.
    /// </summary>
    public Vector2 Target =>
        IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(BodyAUid).WorldPosition;

    private float _invMassB;
    private float _invIB;
    private Vector2 _rB;
    private Vector2 _C;
    private Matrix22 _mass;
    private Vector2 _impulse;
    private float _beta;
    private float _gamma;

    public MouseJoint() {}

    public MouseJoint(EntityUid uidA, EntityUid uidB, Vector2 localAnchorA, Vector2 localAnchorB)
    {
        BodyAUid = uidA;
        BodyBUid = uidB;
        LocalAnchorA = localAnchorA;
        LocalAnchorB = localAnchorB;
    }

    internal MouseJoint(MouseJointState state) : base(state)
    {
        Damping = state.Damping;
        Stiffness = state.Stiffness;
        MaxForce = state.MaxForce;
    }

    public override JointState GetState()
    {
        var mouseState = new MouseJointState
        {
            Damping = _damping,
            Stiffness = _stiffness,
            MaxForce = _maxForce,
            LocalAnchorA = LocalAnchorA,
            LocalAnchorB = LocalAnchorB
        };

        base.GetState(mouseState);
        return mouseState;
    }

    public override Vector2 GetReactionForce(float invDt)
    {
        return _impulse * invDt;
    }

    public override float GetReactionTorque(float invDt)
    {
        return invDt * 0f;
    }

    private int _indexB;
    private Vector2 _localCenterB;

    internal override void InitVelocityConstraints(SolverData data, PhysicsComponent bodyA, PhysicsComponent bodyB)
    {
        _indexB = bodyB.IslandIndex[data.IslandIndex];
        _localCenterB = bodyB.LocalCenter;
        _invMassB = bodyB.InvMass;
        _invIB = bodyB.InvI;

        var cB = data.Positions[_indexB];
        var aB = data.Angles[_indexB];
        var vB = data.LinearVelocities[_indexB];
        var wB = data.AngularVelocities[_indexB];

        Quaternion2D qB = new(aB);

        float d = _damping;
        float k = _stiffness;

        // magic formulas
        // gamma has units of inverse mass.
        // beta has units of inverse time.
        float h = data.FrameTime;
        _gamma = h * (d + h * k);
        if (_gamma != 0.0f)
        {
            _gamma = 1.0f / _gamma;
        }

        _beta = h * k * _gamma;

        // Compute the effective mass matrix.
        _rB = Transform.Mul(qB, LocalAnchorB - _localCenterB);

        // K    = [(1/m1 + 1/m2) * eye(2) - skew(r1) * invI1 * skew(r1) - skew(r2) * invI2 * skew(r2)]
        //      = [1/m1+1/m2     0    ] + invI1 * [r1.y*r1.y -r1.x*r1.y] + invI2 * [r1.y*r1.y -r1.x*r1.y]
        //        [    0     1/m1+1/m2]           [-r1.x*r1.y r1.x*r1.x]           [-r1.x*r1.y r1.x*r1.x]
        Matrix22 K;
        K.EX.X = _invMassB + _invIB * _rB.Y * _rB.Y + _gamma;
        K.EX.Y = -_invIB * _rB.X * _rB.Y;
        K.EY.X = K.EX.Y;
        K.EY.Y = _invMassB + _invIB * _rB.X * _rB.X + _gamma;

        _mass = K.GetInverse();

        _C = cB + _rB - Target;
        _C *= _beta;

        // Cheat with some damping
        wB *= 0.98f;

        if (data.WarmStarting)
        {
            _impulse *= data.DtRatio;
            vB += _impulse * _invMassB;
            wB += _invIB * Vector2.Cross(_rB, _impulse);
        }
        else
        {
            _impulse = Vector2.Zero;
        }

        data.LinearVelocities[_indexB] = vB;
        data.AngularVelocities[_indexB] = wB;
    }

    internal override void SolveVelocityConstraints(SolverData data)
    {
        var vB = data.LinearVelocities[_indexB];
        var wB = data.AngularVelocities[_indexB];

        // Cdot = v + cross(w, r)
        var Cdot = vB + Vector2.Cross(wB, _rB);
        var impulse = Transform.Mul(_mass, -(Cdot + _C + _impulse * _gamma));

        var oldImpulse = _impulse;
        _impulse += impulse;
        float maxImpulse = data.FrameTime * _maxForce;

        if (_impulse.LengthSquared > maxImpulse * maxImpulse)
        {
            _impulse *= maxImpulse / _impulse.Length;
        }
        impulse = _impulse - oldImpulse;

        vB += impulse * _invMassB;
        wB += _invIB * Vector2.Cross(_rB, impulse);

        data.LinearVelocities[_indexB] = vB;
        data.AngularVelocities[_indexB] = wB;
    }

    internal override bool SolvePositionConstraints(SolverData data)
    {
        return true;
    }

    public bool Equals(MouseJoint? other)
    {
        if (other == null) return false;

        return BodyAUid == other.BodyAUid &&
               BodyBUid == other.BodyBUid &&
               _damping.Equals(other.Damping) &&
               _stiffness.Equals(other.Stiffness) &&
               _maxForce.Equals(other.MaxForce);
    }
}
