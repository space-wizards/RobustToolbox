using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Raised on an entity and all entities contained inside it after its container hierarchy changes.
/// </summary>
/// <param name="ContainerOwner">The owner of the container that was directly modified.</param>
/// <param name="Added">True if the entity subtree was inserted into the container, false if it was removed.</param>
[PublicAPI, ByRefEvent]
public readonly record struct EntContainerHierarchyChangedMessage(EntityUid ContainerOwner, bool Added);
