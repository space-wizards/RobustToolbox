using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics
{
    // Real pros use the system messages
    public sealed class PhysicsWakeMessage : EntitySystemMessage
    {
        public PhysicsComponent Body { get; }

        public PhysicsWakeMessage(PhysicsComponent component)
        {
            Body = component;
        }
    }

    public sealed class PhysicsSleepMessage : EntitySystemMessage
    {
        public PhysicsComponent Body { get; }

        public PhysicsSleepMessage(PhysicsComponent component)
        {
            Body = component;
        }
    }
}
