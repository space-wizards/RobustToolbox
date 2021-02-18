namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Raised when an entity parent is changed.
    /// </summary>
    public class EntParentChangedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     Entity that was adopted. The transform component has a property with the new parent.
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        ///     Old parent that abandoned the Entity.
        /// </summary>
        public IEntity? OldParent { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="EntParentChangedMessage"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="oldParent"></param>
        public EntParentChangedMessage(IEntity entity, IEntity? oldParent)
        {
            Entity = entity;
            OldParent = oldParent;
        }
    }
}
