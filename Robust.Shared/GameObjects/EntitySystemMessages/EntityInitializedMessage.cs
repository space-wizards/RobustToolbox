using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.GameObjects.EntitySystemMessages
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