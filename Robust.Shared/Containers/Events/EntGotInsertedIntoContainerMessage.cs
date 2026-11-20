using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Directed at the entity that was inserted successfully.
/// </summary>
[PublicAPI]
public sealed class EntGotInsertedIntoContainerMessage(EntityUid entity, BaseContainer container, ContainerActionContext? context = null) :
    ContainerModifiedMessage(entity, container, context);
