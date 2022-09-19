using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Events;

[ByRefEvent]
public struct PreventCollideEvent
{
    public PhysicsComponent BodyA;
    public PhysicsComponent BodyB;
    public bool Cancelled = false;

    public PreventCollideEvent(PhysicsComponent ourBody, PhysicsComponent otherBody)
    {
        BodyA = ourBody;
        BodyB = otherBody;
    }
}
