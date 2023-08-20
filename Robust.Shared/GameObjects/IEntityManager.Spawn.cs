using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    // This method will soon be marked as obsolete.
    EntityUid[] SpawnEntities(EntityCoordinates coordinates, List<string?> protoNames)
        => SpawnEntitiesAttachedTo(coordinates, protoNames);

    // This method will soon be marked as obsolete.
    EntityUid SpawnEntity(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
        => SpawnAttachedTo(protoName, coordinates, overrides);

    // This method will soon be marked as obsolete.
    EntityUid SpawnEntity(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null)
        => Spawn(protoName, coordinates, overrides);

    EntityUid[] SpawnEntities(MapCoordinates coordinates, params string?[] protoNames);
    EntityUid[] SpawnEntities(MapCoordinates coordinates, List<string?> protoNames);
    EntityUid[] SpawnEntitiesAttachedTo(EntityCoordinates coordinates, List<string?> protoNames);
    EntityUid[] SpawnEntitiesAttachedTo(EntityCoordinates coordinates, params string?[] protoNames);

    /// <summary>
    /// Spawns an entity in nullspace.
    /// </summary>
    EntityUid Spawn(string? protoName = null, ComponentRegistry? overrides = null);

    /// <summary>
    /// Spawns an entity at a specific world position. The entity will either be parented to the map or a grid.
    /// </summary>
    EntityUid Spawn(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null);

    /// <summary>
    /// Spawns an entity and then parents it to the entity that the given entity coordinates are relative to.
    /// </summary>
    EntityUid SpawnAttachedTo(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null);

    /// <summary>
    /// Resolves the given entity coordinates into world coordinates and spawns an entity at that location. The
    /// entity will either be parented to the map or a grid.
    /// </summary>
    EntityUid SpawnAtPosition(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity inside of a container.
    /// </summary>
    bool TrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity inside of a container. If it fails to insert into the container, it will
    /// instead attempt to spawn the entity next to the target's parent.
    /// </summary>
    public EntityUid SpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity adjacent to some other entity. If the other entity is in a container, this will
    /// attempt to insert the new entity into the same container.
    /// </summary>
    bool TrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        ComponentRegistry? overrides = null);

    /// <summary>
    /// Attempts to spawn an entity adjacent to some other entity. If the other entity is in a container, this will
    /// attempt to insert the new entity into the same container. If it fails to insert into the container, it will
    /// instead attempt to spawn the entity next to the target's parent.
    /// </summary>
    EntityUid SpawnNextToOrDrop(string? protoName, EntityUid target, ComponentRegistry? overrides = null);
}