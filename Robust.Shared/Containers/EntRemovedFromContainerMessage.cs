using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    [PublicAPI]
    public abstract class ContainerModifiedMessage : EntitySystemMessage
    {
        protected ContainerModifiedMessage(IEntity entity, IContainer container)
        {
            Entity = entity;
            Container = container;
        }

        /// <summary>
        ///     The entity that was removed or inserted from/into the container.
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        ///     The container being acted upon.
        /// </summary>
        public IContainer Container { get; }
    }

    /// <summary>
    ///     Raised when an entity is removed from a container.
    /// </summary>
    [PublicAPI]
    public sealed class EntRemovedFromContainerMessage : ContainerModifiedMessage
    {
        public EntRemovedFromContainerMessage(IEntity entity, IContainer container) : base(entity, container)
        {
        }
    }

    /// <summary>
    ///     Raised when an entity is inserted into a container.
    /// </summary>
    [PublicAPI]
    public sealed class EntInsertedIntoContainerMessage : ContainerModifiedMessage
    {
        public EntInsertedIntoContainerMessage(IEntity entity, IContainer container) : base(entity, container)
        {
        }
    }
}
