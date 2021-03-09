namespace Robust.Shared.GameObjects.EntitySystemMessages
{
    /// <summary>
    ///     Raised right before an entity will be deleted.
    /// </summary>
    public class EntityDeletedMessage : EntitySystemMessage
    {
        public IEntity Entity { get; }

        public EntityDeletedMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}
