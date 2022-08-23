using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Raised when an entity is removed from a container. Directed at the container. This gets called BEFORE the entity gets re-parented. I.e., the current parent is the container.
    /// </summary>
    [PublicAPI]
    public sealed class EntRemovedFromContainerMessage : ContainerModifiedMessage
    {
        public EntRemovedFromContainerMessage(EntityUid entity, IContainer container) : base(entity, container) { }
    }

    /// <summary>
    /// Raised when an entity is removed from a container. Directed at the entity. This gets called BEFORE the entity gets re-parented. I.e., the current parent is the container.
    /// </summary>
    [PublicAPI]
    public sealed class EntGotRemovedFromContainerMessage : ContainerModifiedMessage
    {
        public EntGotRemovedFromContainerMessage(EntityUid entity, IContainer container) : base(entity, container) { }
    }
}
