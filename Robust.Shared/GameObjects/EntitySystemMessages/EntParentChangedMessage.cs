namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Raised when an entity parent is changed.
    /// </summary>
    [ByRefEvent]
    public class EntParentChangedMessage : EntityEventArgs
    {
        /// <summary>
        ///     Entity that was adopted. The transform component has a property with the new parent.
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     Old parent that abandoned the Entity.
        /// </summary>
        public EntityUid? OldParent { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="EntParentChangedMessage"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="oldParent"></param>
        public EntParentChangedMessage(EntityUid entity, EntityUid? oldParent)
        {
            Entity = entity;
            OldParent = oldParent;
        }
    }
}
