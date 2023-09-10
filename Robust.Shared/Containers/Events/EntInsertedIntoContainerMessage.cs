using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Raised when an entity is inserted into a container. This is raised AFTER the entity has been re-parented. I.e., the current parent is the container.
    /// </summary>
    [PublicAPI]
    public sealed class EntInsertedIntoContainerMessage : ContainerModifiedMessage
    {
        public readonly EntityUid OldParent;

        public EntInsertedIntoContainerMessage(EntityUid entity, EntityUid oldParent, BaseContainer container) : base(entity, container)
        {
            OldParent = oldParent;
        }
    }
}
