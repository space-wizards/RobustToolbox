using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
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
