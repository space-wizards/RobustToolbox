namespace Robust.Shared.GameObjects
{
    public sealed class EntityInitializedMessage : EntityEventArgs
    {
        public IEntity Entity { get; }
        
        public EntityInitializedMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}