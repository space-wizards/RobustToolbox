using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.GameObjects.EntitySystemMessages
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