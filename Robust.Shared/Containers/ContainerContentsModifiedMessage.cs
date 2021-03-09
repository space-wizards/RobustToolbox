using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// The contents of this container have been changed.
    /// </summary>
    public class ContainerContentsModifiedMessage : ComponentMessage
    {
        /// <summary>
        /// Container whose contents were modified.
        /// </summary>
        public IContainer Container { get; }

        /// <summary>
        /// Entity that was added or removed from the container.
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        /// If true, the entity was removed. If false, it was added to the container.
        /// </summary>
        public bool Removed { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ContainerContentsModifiedMessage"/>.
        /// </summary>
        /// <param name="container">Container whose contents were modified.</param>
        /// <param name="entity">Entity that was added or removed in the container.</param>
        /// <param name="removed">If true, the entity was removed. If false, it was added to the container.</param>
        public ContainerContentsModifiedMessage(IContainer container, IEntity entity, bool removed)
        {
            Container = container;
            Entity = entity;
            Removed = removed;
        }
    }
}