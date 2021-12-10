namespace Robust.Shared.GameObjects
{
    public sealed class EntityDeletedMessage : EntityEventArgs
    {
        public EntityUid Entity { get; }

        public EntityDeletedMessage(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
