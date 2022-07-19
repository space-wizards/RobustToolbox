namespace Robust.Shared.GameObjects
{
    public readonly struct EntityInitializedMessage
    {
        public EntityUid Entity { get; }

        public EntityInitializedMessage(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
