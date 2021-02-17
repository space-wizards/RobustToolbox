using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    public class RelayMovementEntityMessage : ComponentMessage
    {
        [PublicAPI]
        public readonly IEntity Entity;

        public RelayMovementEntityMessage(IEntity entity)
        {
            Entity = entity;
        }
    }

    /// <summary>
    ///     The entity transform parent has been changed.
    /// </summary>
    public class ParentChangedMessage : ComponentMessage
    {
        /// <summary>
        ///     The new parent of the transform.
        /// </summary>
        public IEntity? NewParent { get; }

        /// <summary>
        ///     The old parent of the transform.
        /// </summary>
        public IEntity? OldParent { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="ParentChangedMessage"/>.
        /// </summary>
        /// <param name="newParent">The new parent of the transform.</param>
        /// <param name="oldParent">The old parent of the transform.</param>
        public ParentChangedMessage(IEntity? newParent, IEntity? oldParent)
        {
            NewParent = newParent;
            OldParent = oldParent;
        }
    }
}
