namespace Robust.Shared.GameObjects
{
    public sealed class EntityInitializedMessage : EntityEventArgs
    {
        public EntityUid Entity { get; }
        
        public EntityInitializedMessage(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
