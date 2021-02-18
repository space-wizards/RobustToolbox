using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    public sealed class EntityDeletedMessage : EntitySystemMessage
    {
        public IEntity Entity { get; }
        
        public EntityDeletedMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}
