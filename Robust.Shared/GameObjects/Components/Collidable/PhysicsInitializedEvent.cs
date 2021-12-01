namespace Robust.Shared.GameObjects
{
    public readonly struct PhysicsInitializedEvent
    {
        public readonly EntityUid Uid;

        public PhysicsInitializedEvent(EntityUid uid)
        {
            Uid = uid;
        }
    }
}
