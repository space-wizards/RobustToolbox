namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// The children of this entity are about to be deleted.
    /// </summary>
    [ByRefEvent]
    public readonly struct EntityTerminatingEvent
    {
        public readonly EntityUid Entity;

        public EntityTerminatingEvent(EntityUid entity)
        {
            Entity = entity;
        }
    }
}
