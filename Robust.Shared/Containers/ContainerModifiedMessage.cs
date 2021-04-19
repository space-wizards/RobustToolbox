using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Raised when the contents of a container have been modified.
    /// </summary>
    [PublicAPI]
    public abstract class ContainerModifiedMessage : EntityEventArgs
    {
        /// <summary>
        /// The container being acted upon.
        /// </summary>
        public IContainer Container { get; }

        /// <summary>
        /// The entity that was removed or inserted from/into the container.
        /// </summary>
        public IEntity Entity { get; }

        protected ContainerModifiedMessage(IEntity entity, IContainer container)
        {
            Entity = entity;
            Container = container;
        }
    }
}
