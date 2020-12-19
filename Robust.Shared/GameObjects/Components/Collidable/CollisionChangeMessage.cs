namespace Robust.Shared.GameObjects.Components
{
    public class CollisionChangeMessage : EntitySystemMessage
    {
        public PhysicsComponent PhysicsComponent { get; }
        public bool Enabled { get; }

        public CollisionChangeMessage(PhysicsComponent physicsComponent, bool enabled)
        {
            PhysicsComponent = physicsComponent;
            Enabled = enabled;
        }
    }
}
