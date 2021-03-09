namespace Robust.Shared.GameObjects.EntitySystemMessages
{
    public class DirtyEntityMessage : EntitySystemMessage
    {
        public IEntity Entity { get; }

        public DirtyEntityMessage(IEntity entity)
        {
            Entity = entity;
        }
    }
}
