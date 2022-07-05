namespace Robust.Shared.GameObjects
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
