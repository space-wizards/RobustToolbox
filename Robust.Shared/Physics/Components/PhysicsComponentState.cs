using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Components;

[Serializable, NetSerializable]
public readonly record struct PhysicsComponentState(
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

    public readonly Vector2 LinearVelocity = LinearVelocity;
    public readonly float AngularVelocity = AngularVelocity;
    public readonly BodyType BodyType = BodyType;

    public readonly float Friction = Friction;
    public readonly float LinearDamping = LinearDamping;
    public readonly float AngularDamping = AngularDamping;
}
