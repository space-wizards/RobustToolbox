namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// The children of this entity are about to be deleted.
    /// </summary>
    [ByRefEvent]
    public readonly struct EntityTerminatingEvent(Entity<MetaDataComponent> entity)
    {
        public readonly Entity<MetaDataComponent> Entity = entity;
    }
}
