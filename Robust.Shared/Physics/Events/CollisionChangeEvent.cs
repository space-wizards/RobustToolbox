using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Events
{
    [ByRefEvent]
    public readonly struct CollisionChangeEvent
    {
        public readonly PhysicsComponent Body;

        public readonly bool CanCollide;

        public CollisionChangeEvent(PhysicsComponent body, bool canCollide)
        {
            Body = body;
            CanCollide = canCollide;
        }
    }
}
