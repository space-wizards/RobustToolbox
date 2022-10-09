using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Directed at the entity that was inserted successfully.
/// </summary>
[PublicAPI]
public sealed class GotInsertedEvent : ContainerModifiedMessage
{
    public GotInsertedEvent(EntityUid entity, IContainer container) : base(entity, container) { }
}
