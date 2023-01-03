using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Robust.Shared.Physics;

public sealed class JointAddedEvent : EntityEventArgs
{
    public PhysicsComponent OurBody { get; }

    public PhysicsComponent OtherBody { get; }

    public Joint Joint { get; }

    public JointAddedEvent(Joint joint, PhysicsComponent ourBody, PhysicsComponent otherBody)
    {
        Joint = joint;
        OurBody = ourBody;
        OtherBody = otherBody;
    }
}

public sealed class JointRemovedEvent : EntityEventArgs
{
    public PhysicsComponent OurBody { get; }

    public PhysicsComponent OtherBody { get; }

    public Joint Joint { get; }

    public JointRemovedEvent(Joint joint, PhysicsComponent ourBody, PhysicsComponent otherBody)
    {
        Joint = joint;
        OurBody = ourBody;
        OtherBody = otherBody;
    }
}
