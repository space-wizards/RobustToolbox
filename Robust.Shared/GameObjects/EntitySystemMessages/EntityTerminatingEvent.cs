namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// The children of this entity are about to be deleted.
    /// </summary>
    [ByRefEvent]
    public struct EntityTerminatingEvent
    {
        public EntityUid Owner;

        public EntityTerminatingEvent(EntityUid uid)
        {
            Owner = uid;
        }
    }
}
