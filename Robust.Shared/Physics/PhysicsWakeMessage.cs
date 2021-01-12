using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Robust.Shared.Physics
{
    internal sealed class PhysicsWakeMessage : EntitySystemMessage
    {
        public PhysicsComponent Body { get; }

        public PhysicsWakeMessage(PhysicsComponent component)
        {
            Body = component;
        }
    }

    internal sealed class PhysicsSleepMessage : EntitySystemMessage
    {
        public PhysicsComponent Body { get; }

        public PhysicsSleepMessage(PhysicsComponent component)
        {
            Body = component;
        }
    }
}
