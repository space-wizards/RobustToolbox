using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects.Components
{
    public class CollisionChangeMessage : EntitySystemMessage
    {
        // TODO
        public PhysicsComponent PhysicsComponent { get; }
        public bool Enabled { get; }

        public CollisionChangeMessage(PhysicsComponent physicsComponent, bool enabled)
        {
            PhysicsComponent = physicsComponent;
            Enabled = enabled;
        }
    }
}
