using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Components;

[Serializable, NetSerializable]
public sealed class PhysicsComponentState(
        bool CanCollide,
        bool SleepingAllowed,
        bool FixedRotation,
        BodyStatus Status,
        Vector2 LinearVelocity,
        float AngularVelocity,
        BodyType BodyType,
        float Friction,
        float LinearDamping,
        float AngularDamping)
    : IComponentState
{
    public readonly bool CanCollide = CanCollide;
    public readonly bool SleepingAllowed = SleepingAllowed;
    public readonly bool FixedRotation = FixedRotation;
    public readonly BodyStatus Status = Status;

    public Vector2 LinearVelocity = LinearVelocity;
    public float AngularVelocity = AngularVelocity;
    public readonly BodyType BodyType = BodyType;

    public readonly float Friction = Friction;
    public readonly float LinearDamping = LinearDamping;
    public readonly float AngularDamping = AngularDamping;
}

[Serializable, NetSerializable]
public sealed class PhysicsDeltaState : IComponentState, IComponentDeltaState
{
    public Vector2 LinearVelocity;
    public float AngularVelocity;

    public bool FullState => true;

    public void ApplyToFullState(IComponentState fullState)
    {
        var state = (PhysicsComponentState)fullState;

        state.LinearVelocity = LinearVelocity;
        state.AngularVelocity = AngularVelocity;
    }

    public IComponentState CreateNewFullState(IComponentState fullState)
    {
        var state = (PhysicsComponentState) fullState;
        return new PhysicsComponentState(state.CanCollide, state.SleepingAllowed, state.FixedRotation, state.Status,
            LinearVelocity, AngularVelocity, state.BodyType, state.Friction, state.LinearDamping, state.AngularDamping);
    }
}
