using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Robust.Shared.Physics;

public sealed class JointAddedEvent : EntityEventArgs
{
    public readonly EntityUid OurEntity;

    public readonly EntityUid OtherEntity;

    public PhysicsComponent OurBody { get; }

    public PhysicsComponent OtherBody { get; }

    public Joint Joint { get; }

    public JointAddedEvent(
        Joint joint,
        EntityUid ourEntity,
        EntityUid otherEntity,
        PhysicsComponent ourBody,
        PhysicsComponent otherBody)
    {
        Joint = joint;
        OurEntity = ourEntity;
        OtherEntity = otherEntity;
        OurBody = ourBody;
        OtherBody = otherBody;
    }
}

public sealed class JointRemovedEvent : EntityEventArgs
{
    public readonly EntityUid OurEntity;

    public readonly EntityUid OtherEntity;

    public PhysicsComponent OurBody { get; }

    public PhysicsComponent OtherBody { get; }

    public Joint Joint { get; }

    public JointRemovedEvent(
        Joint joint,
        EntityUid ourEntity,
        EntityUid otherEntity,
        PhysicsComponent ourBody,
        PhysicsComponent otherBody)
    {
        Joint = joint;
        OurEntity = ourEntity;
        OtherEntity = otherEntity;
        OurBody = ourBody;
        OtherBody = otherBody;
    }
}
