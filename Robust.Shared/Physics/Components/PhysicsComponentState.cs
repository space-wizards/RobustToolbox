using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Components;

/// <summary>
/// No chunky fixture updates
/// </summary>
[Serializable, NetSerializable]
public record struct PhysicsSlimDeltaState : IComponentDeltaState<PhysicsComponentState>
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

    public void ApplyToFullState(PhysicsComponentState fullState)
    {
        fullState.Slim = this;
    }

    public PhysicsComponentState CreateNewFullState(PhysicsComponentState fullState)
    {
        var copy = new PhysicsComponentState(fullState)
        {
            Slim = this
        };
        return copy;
    }
}


/// <summary>
/// Average use-case of only linear velocity update
/// </summary>
[Serializable, NetSerializable]
public record struct PhysicsLinearVelocityDeltaState : IComponentDeltaState<PhysicsComponentState>
{
    public Vector2 LinearVelocity;

    public void ApplyToFullState(PhysicsComponentState fullState)
    {
        fullState.Slim.LinearVelocity = LinearVelocity;
    }

    public PhysicsComponentState CreateNewFullState(PhysicsComponentState fullState)
    {
        var copy = new PhysicsComponentState(fullState);
        copy.Slim.LinearVelocity = LinearVelocity;
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
        fullState.Slim.LinearVelocity = LinearVelocity;
        fullState.Slim.AngularVelocity = AngularVelocity;
    }

    public PhysicsComponentState CreateNewFullState(PhysicsComponentState fullState)
    {
        var copy = new PhysicsComponentState(fullState);
        copy.Slim.LinearVelocity = LinearVelocity;
        copy.Slim.AngularVelocity = AngularVelocity;
        return copy;
    }
}

[Serializable, NetSerializable]
public sealed class PhysicsComponentState : IComponentState
{
    public PhysicsSlimDeltaState Slim;

    public Dictionary<string, Fixture> Fixtures = new();

    public PhysicsComponentState() {}

    public PhysicsComponentState(PhysicsComponentState existing)
    {
        Slim = existing.Slim;

        foreach (var (key, value) in existing.Fixtures)
        {
            Fixtures[key] = new(value);
        }
    }
}
