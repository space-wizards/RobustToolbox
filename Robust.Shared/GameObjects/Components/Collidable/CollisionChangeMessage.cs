namespace Robust.Shared.GameObjects
{
    public class CollisionChangeMessage : EntityEventArgs
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
