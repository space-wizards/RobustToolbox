namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// The children of this entity are about to be deleted.
    /// </summary>
    public sealed class EntityTerminatingEvent : EntityEventArgs
    {
        public EntityUid Owner;

        public EntityTerminatingEvent(EntityUid uid)
        {
            Owner = uid;
        }
    }
}
