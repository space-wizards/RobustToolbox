using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

/// <summary>
///     Arguments for when a map is created or deleted locally ore remotely.
/// </summary>
public sealed class MapEventArgs : EventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public MapEventArgs(MapId map)
    {
        Map = map;
    }

    /// <summary>
    ///     Map that is being modified.
    /// </summary>
    public MapId Map { get; }
}

internal partial class MapManager
{
    private readonly Dictionary<MapId, EntityUid> _mapEntities = new();
    private MapId _highestMapId = MapId.Nullspace;

    /// <inheritdoc />
    public virtual void DeleteMap(MapId mapId)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        if (!_mapEntities.TryGetValue(mapId, out var ent) || !ent.IsValid())
            throw new InvalidOperationException($"Attempted to delete nonexistent map '{mapId}'");

        EntityManager.DeleteEntity(ent);
    }

    /// <inheritdoc />
    public MapId CreateMap(MapId? mapId = null)
    {
        return ((IMapManagerInternal) this).CreateMap(mapId, default);
    }

    /// <inheritdoc />
    public bool MapExists(MapId mapId)
    {
        return _mapEntities.ContainsKey(mapId);
    }

    /// <inheritdoc />
    public EntityUid CreateNewMapEntity(MapId mapId)
    {
        DebugTools.Assert(mapId != MapId.Nullspace);
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        var newEntity = EntityManager.CreateEntityUninitialized(null);
        SetMapEntity(mapId, newEntity);

        EntityManager.InitializeComponents(newEntity);
        EntityManager.StartComponents(newEntity);

        return newEntity;
    }

    /// <inheritdoc />
    public void SetMapEntity(MapId mapId, EntityUid newMapEntity, bool updateChildren = true)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        if (!_mapEntities.ContainsKey(mapId))
            throw new InvalidOperationException($"Map {mapId} does not exist.");

        foreach (var kvEntity in _mapEntities)
        {
            if (kvEntity.Value == newMapEntity)
            {
                if (mapId == kvEntity.Key)
                    return;

                throw new InvalidOperationException(
                    $"Entity {newMapEntity} is already the root node of another map {kvEntity.Key}.");
            }
        }

        MapComponent? mapComp;
        // If this is being done as part of maploader then we want to copy the preinit state across mainly.
        bool preInit = false;
        bool paused = false;

        // remove existing graph
        if (_mapEntities.TryGetValue(mapId, out var oldEntId))
        {
            if (EntityManager.TryGetComponent(oldEntId, out mapComp))
            {
                preInit = mapComp.MapPreInit;
                paused = mapComp.MapPaused;
            }

            EntityManager.System<SharedTransformSystem>().ReparentChildren(oldEntId, newMapEntity);

            //Note: EntityUid.Invalid gets passed in here
            //Note: This prevents setting a subgraph as the root, since the subgraph will be deleted
            EntityManager.DeleteEntity(oldEntId);
        }

        var raiseEvent = false;

        // re-use or add map component
        if (!EntityManager.TryGetComponent(newMapEntity, out mapComp))
            mapComp = EntityManager.AddComponent<MapComponent>(newMapEntity);
        else
        {
            raiseEvent = true;

            if (mapComp.MapId != mapId)
            {
                _sawmill.Warning($"Setting map {mapId} root to entity {newMapEntity}, but entity thinks it is root node of map {mapComp.MapId}.");
            }
        }

        _sawmill.Debug($"Setting map {mapId} entity to {newMapEntity}");

        // set as new map entity
        mapComp.MapPreInit = preInit;
        mapComp.MapPaused = paused;

        mapComp.MapId = mapId;
        _mapEntities[mapId] = newMapEntity;

        // Yeah this sucks but I just want to save maps for now, deal.
        if (raiseEvent)
        {
            var ev = new MapChangedEvent(newMapEntity, mapId, true);
            EntityManager.EventBus.RaiseLocalEvent(newMapEntity, ev, true);
        }
    }

    /// <inheritdoc />
    public EntityUid GetMapEntityId(MapId mapId)
    {
        if (_mapEntities.TryGetValue(mapId, out var entId))
            return entId;

        return EntityUid.Invalid;
    }

    /// <summary>
    ///     Replaces GetMapEntity()'s throw-on-failure semantics.
    /// </summary>
    public EntityUid GetMapEntityIdOrThrow(MapId mapId)
    {
        return _mapEntities[mapId];
    }

    /// <inheritdoc />
    public bool HasMapEntity(MapId mapId)
    {
        return _mapEntities.ContainsKey(mapId);
    }

    /// <inheritdoc />
    public IEnumerable<MapId> GetAllMapIds()
    {
        return _mapEntities.Keys;
    }

    /// <inheritdoc />
    public bool IsMap(EntityUid uid)
    {
        return EntityManager.HasComponent<MapComponent>(uid);
    }

    /// <inheritdoc />
    public MapId NextMapId()
    {
        return _highestMapId = new MapId(_highestMapId.Value + 1);
    }

    MapId IMapManagerInternal.CreateMap(MapId? mapId, EntityUid entityUid)
    {
        if (mapId == MapId.Nullspace)
            throw new InvalidOperationException("Attempted to create a null-space map.");

#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        var actualId = mapId ?? new MapId(_highestMapId.Value + 1);

        if (MapExists(actualId))
            throw new InvalidOperationException($"A map with ID {actualId} already exists");

        if (_highestMapId.Value < actualId.Value)
            _highestMapId = actualId;

        _sawmill.Info($"Creating new map {actualId}");

        if (actualId != MapId.Nullspace) // nullspace isn't bound to an entity
        {
            Entity<MapComponent> result = default;
            var query = EntityManager.AllEntityQueryEnumerator<MapComponent>();
            while (query.MoveNext(out var uid, out var map))
            {
                if (map.MapId != actualId)
                    continue;

                result = (uid, map);
                break;
            }

            if (result != default)
            {
                DebugTools.Assert(mapId != null);
                _mapEntities.Add(actualId, result);
                _sawmill.Debug($"Rebinding map {actualId} to entity {result.Owner}");
            }
            else
            {
                var newEnt = EntityManager.CreateEntityUninitialized(null, entityUid);
                _mapEntities.Add(actualId, newEnt);

                var mapComp = EntityManager.AddComponent<MapComponent>(newEnt);
                mapComp.MapId = actualId;
                var meta = EntityManager.GetComponent<MetaDataComponent>(newEnt);
                EntityManager.System<MetaDataSystem>().SetEntityName(newEnt, $"map {actualId}", meta);
                EntityManager.Dirty(newEnt, mapComp, meta);
                EntityManager.InitializeComponents(newEnt, meta);
                EntityManager.StartComponents(newEnt);
                _sawmill.Debug($"Binding map {actualId} to entity {newEnt}");
            }
        }

        return actualId;
    }
}
