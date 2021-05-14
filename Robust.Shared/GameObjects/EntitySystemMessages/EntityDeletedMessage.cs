namespace Robust.Shared.GameObjects
{
    public sealed class EntityDeletedMessage : EntityEventArgs
    {
        public IEntity Entity { get; }
        
        public EntityDeletedMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}
