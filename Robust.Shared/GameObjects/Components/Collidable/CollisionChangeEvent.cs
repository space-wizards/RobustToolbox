namespace Robust.Shared.GameObjects.Components
{
    public class CollisionChangeEvent : EntitySystemMessage
    {
        public EntityUid Owner { get; }
        public bool CanCollide { get; }

        public CollisionChangeEvent(EntityUid owner, bool canCollide)
        {
            Owner = owner;
            CanCollide = canCollide;
        }
    }
}