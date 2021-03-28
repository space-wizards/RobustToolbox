using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Raised when an entity is inserted into a container.
    /// </summary>
    [PublicAPI]
    public sealed class EntInsertedIntoContainerMessage : ContainerModifiedMessage
    {
        public EntInsertedIntoContainerMessage(IEntity entity, IContainer container) : base(entity, container) { }
    }
}
