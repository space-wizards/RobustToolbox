using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Components;

/// <summary>
/// Average use-case of only linear velocity update
/// </summary>
[Serializable, NetSerializable]
public record struct PhysicsLinearVelocityDeltaState : IComponentDeltaState<PhysicsComponentState>
{
    public Vector2 LinearVelocity;

    public void ApplyToFullState(PhysicsComponentState fullState)
    {
        fullState.LinearVelocity = LinearVelocity;
    }

    public PhysicsComponentState CreateNewFullState(PhysicsComponentState fullState)
    {
        var copy = new PhysicsComponentState(fullState)
        {
            LinearVelocity = LinearVelocity,
        };
        return copy;
    }
}

/// <summary>
/// 2nd-most typical usecase of just velocity updates
/// </summary>
[Serializable, NetSerializable]
public record struct PhysicsVelocityDeltaState : IComponentDeltaState<PhysicsComponentState>
{
    public Vector2 LinearVelocity;
    public float AngularVelocity;

    public void ApplyToFullState(PhysicsComponentState fullState)
    {
        fullState.LinearVelocity = LinearVelocity;
        fullState.AngularVelocity = AngularVelocity;
    }

    public PhysicsComponentState CreateNewFullState(PhysicsComponentState fullState)
    {
        var copy = new PhysicsComponentState(fullState)
        {
            LinearVelocity = LinearVelocity,
            AngularVelocity = AngularVelocity
        };
        return copy;
    }
}

[Serializable, NetSerializable]
public sealed class PhysicsComponentState : IComponentState
{
    public bool CanCollide;
    public bool SleepingAllowed;
    public bool FixedRotation;
    public BodyStatus Status;

    public Vector2 LinearVelocity;
    public float AngularVelocity;
    public BodyType BodyType;

    public float Friction;
    public float LinearDamping;
    public float AngularDamping;

    public Vector2 Force;
    public float Torque;

    public PhysicsComponentState() {}

    public PhysicsComponentState(PhysicsComponentState existing)
    {
        CanCollide = existing.CanCollide;
        SleepingAllowed = existing.SleepingAllowed;
        FixedRotation = existing.FixedRotation;
        Status = existing.Status;

        LinearVelocity = existing.LinearVelocity;
        AngularVelocity = existing.AngularVelocity;
        BodyType = existing.BodyType;

        Friction = existing.Friction;
        LinearDamping = existing.LinearDamping;
        AngularDamping = existing.AngularDamping;

        Force = existing.Force;
        Torque = existing.Torque;
    }
}
