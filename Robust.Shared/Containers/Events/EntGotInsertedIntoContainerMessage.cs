using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Directed at the entity that was inserted successfully.
/// </summary>
[PublicAPI]
public sealed class EntGotInsertedIntoContainerMessage : ContainerModifiedMessage
{
    public EntGotInsertedIntoContainerMessage(EntityUid entity, IContainer container) : base(entity, container) { }
}
