using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    // This method will soon be marked as obsolete.
    EntityUid[] SpawnEntities(EntityCoordinates coordinates, List<string?> protoNames)
        => SpawnEntitiesAttachedTo(coordinates, protoNames);

    // This method will soon be marked as obsolete.
    EntityUid SpawnEntity(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null);

    // This method will soon be marked as obsolete.
    EntityUid SpawnEntity(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null);

    EntityUid[] SpawnEntities(MapCoordinates coordinates, params string?[] protoNames);
    EntityUid[] SpawnEntities(MapCoordinates coordinates, string? prototype, int count);
    EntityUid[] SpawnEntities(MapCoordinates coordinates, List<string?> protoNames);
    EntityUid[] SpawnEntitiesAttachedTo(EntityCoordinates coordinates, List<string?> protoNames);
    EntityUid[] SpawnEntitiesAttachedTo(EntityCoordinates coordinates, params string?[] protoNames);

    /// <summary>
    /// Spawns an entity in nullspace.
    /// </summary>
    EntityUid Spawn(string? protoName = null, ComponentRegistry? overrides = null, bool doMapInit = true);

    /// <summary>
    /// Spawns an entity at a specific world position. The entity will either be parented to the map or a grid.
    /// </summary>
    EntityUid Spawn(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default!);

    /// <summary>
    /// Spawns an entity and then parents it to the entity that the given entity coordinates are relative to.
    /// </summary>
    EntityUid SpawnAttachedTo(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default);

    /// <summary>
    /// Resolves the given entity coordinates into world coordinates and spawns an entity at that location. The
    /// entity will either be parented to the map or a grid.
    /// </summary>
    EntityUid SpawnAtPosition(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempt to spawn an entity and insert it into a container. If the insertion fails, the entity gets deleted.
    /// </summary>
    bool TrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity inside of a container. If it fails to insert into the container, it will
    /// instead drop the entity next to the target (see <see cref="SpawnNextToOrDrop"/>).
    /// </summary>
    EntityUid SpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null);

    /// <inheritdoc cref="SpawnInContainerOrDrop(string?,Robust.Shared.GameObjects.EntityUid,string,Robust.Shared.GameObjects.TransformComponent?,Robust.Shared.Containers.ContainerManagerComponent?,Robust.Shared.Prototypes.ComponentRegistry?)"/>
    EntityUid SpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        out bool inserted,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity adjacent to some other target entity. If the target entity is in
    /// a container, this will attempt to insert the spawned entity into the same container. If the insertion fails,
    /// the entity is deleted. If the entity is not in a container, this behaves like <see cref="SpawnNextToOrDrop"/>.
    /// </summary>
    bool TrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity adjacent to some other entity. If the other entity is in a container, this will
    /// attempt to insert the new entity into the same container. If it fails to insert into the container, it will
    /// instead attempt to spawn the entity next to the target's parent.
    /// </summary>
    EntityUid SpawnNextToOrDrop(
        string? protoName,
        EntityUid target,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null);
}
