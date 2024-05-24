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

/// <summary>
/// Slower delta state that can include all fields.
/// </summary>
[Serializable, NetSerializable]
public record struct PhysicsDeltaState() : IComponentDeltaState<PhysicsComponentState>
{
    public List<(int Index, object? Value)> Fields = new();

    public void ApplyToFullState(PhysicsComponentState fullState)
    {
        ApplyTo(fullState);
    }

    public PhysicsComponentState CreateNewFullState(PhysicsComponentState fullState)
    {
        var copy = new PhysicsComponentState(fullState);
        ApplyTo(copy);
        return copy;
    }

    private void ApplyTo(PhysicsComponentState state)
    {
        foreach (var (index, value) in Fields)
        {
            switch (index)
            {
                // See SharedPhysicsSystem.
                // If there's an easy way to get a compreg here then we could debug assert.
                case 0:
                    state.CanCollide = (bool)value!;
                    break;
                case 1:
                    state.Status = (BodyStatus)value!;
                    break;
                case 2:
                    state.BodyType = (BodyType)value!;
                    break;
                case 3:
                    state.SleepingAllowed = (bool)value!;
                    break;
                case 4:
                    state.FixedRotation = (bool)value!;
                    break;
                case 5:
                    state.Friction = (float)value!;
                    break;
                case 6:
                    state.Force = (Vector2)value!;
                    break;
                case 7:
                    state.Torque = (float)value!;
                    break;
                case 8:
                    state.LinearDamping = (float)value!;
                    break;
                case 9:
                    state.AngularDamping = (float)value!;
                    break;
                case 10:
                    state.AngularVelocity = (float)value!;
                    break;
                case 11:
                    state.LinearVelocity = (Vector2)value!;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
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
