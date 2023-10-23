namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// The children of this entity are about to be deleted.
    /// </summary>
    [ByRefEvent]
    public readonly struct EntityTerminatingEvent
    {
        public readonly EntityUid Entity;
        public readonly MetaDataComponent Metadata;

        public EntityTerminatingEvent(EntityUid entity, MetaDataComponent metadata)
        {
            Entity = entity;
            Metadata = metadata;
        }
    }
}
