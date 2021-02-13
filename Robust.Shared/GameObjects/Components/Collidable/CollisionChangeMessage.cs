namespace Robust.Shared.GameObjects
{
    public class CollisionChangeMessage : EntitySystemMessage
    {
        public PhysicsComponent Body { get; }

        public EntityUid Owner { get; }
        public bool CanCollide { get; }

        public CollisionChangeMessage(PhysicsComponent body, EntityUid owner, bool canCollide)
        {
            Body = body;
            Owner = owner;
            CanCollide = canCollide;
        }
    }
}
