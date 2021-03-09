namespace Robust.Shared.GameObjects
{
    public sealed class EntityInitializedMessage : EntitySystemMessage
    {
        public IEntity Entity { get; }
        
        public EntityInitializedMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}