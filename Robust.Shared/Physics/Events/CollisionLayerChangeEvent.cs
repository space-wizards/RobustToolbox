using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Events
{
    /// <summary>
    ///     These events are broadcast (not directed) whenever an entity's ability to collide changes.
    /// </summary>
    [ByRefEvent]
    public readonly struct CollisionLayerChangeEvent
    {
        public readonly PhysicsComponent Body;

        public CollisionLayerChangeEvent(PhysicsComponent body)
        {
            Body = body;
        }
    }
}
