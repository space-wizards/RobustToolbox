using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
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

        if (!_mapEntities.TryGetValue(mapId, out var ent))
            throw new InvalidOperationException($"Attempted to delete nonexistent map '{mapId}'");

        if (ent != EntityUid.Invalid)
        {
            EntityManager.DeleteEntity(ent);
        }
        else
        {
            // unbound map
            TrueDeleteMap(mapId);
        }
    }

    /// <inheritdoc />
    public void TrueDeleteMap(MapId mapId)
    {
        // grids are cached because Delete modifies collection
        foreach (var grid in GetAllMapGrids(mapId).ToList())
        {
            DeleteGrid(grid.Index);
        }

        if (mapId != MapId.Nullspace)
        {
            var args = new MapEventArgs(mapId);
            OnMapDestroyedGridTree(args);
            MapDestroyed?.Invoke(this, args);
            _mapEntities.Remove(mapId);
        }
        else
        {
            _mapEntities[mapId] = EntityUid.Invalid;
        }

        Logger.InfoS("map", $"Deleting map {mapId}");
    }

    /// <inheritdoc />
    public MapId CreateMap(MapId? mapId = null)
    {
        return CreateMap(mapId, default);
    }

    /// <inheritdoc />
    public bool MapExists(MapId mapId)
    {
        return _mapEntities.ContainsKey(mapId);
    }

    /// <inheritdoc />
    public EntityUid CreateNewMapEntity(MapId mapId)
    {
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
    public void SetMapEntity(MapId mapId, EntityUid newMapEntity)
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
                throw new InvalidOperationException(
                    $"Entity {newMapEntity} is already the root node of map {kvEntity.Key}.");
            }
        }

        // remove existing graph
        if (_mapEntities.TryGetValue(mapId, out var oldEntId))
        {
            //Note: EntityUid.Invalid gets passed in here
            //Note: This prevents setting a subgraph as the root, since the subgraph will be deleted
            EntityManager.DeleteEntity(oldEntId);
        }
        else
            _mapEntities.Add(mapId, EntityUid.Invalid);

        // re-use or add map component
        if (!EntityManager.TryGetComponent(newMapEntity, out MapComponent? mapComp))
            mapComp = EntityManager.AddComponent<MapComponent>(newMapEntity);
        else
        {
            if (mapComp.WorldMap != mapId)
            {
                Logger.WarningS("map",
                    $"Setting map {mapId} root to entity {newMapEntity}, but entity thinks it is root node of map {mapComp.WorldMap}.");
            }
        }

        Logger.DebugS("map", $"Setting map {mapId} entity to {newMapEntity}");

        // set as new map entity
        mapComp.WorldMap = mapId;
        _mapEntities[mapId] = newMapEntity;
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
        return EntityManager.HasComponent<IMapComponent>(uid);
    }

    /// <inheritdoc />
    public MapId NextMapId()
    {
        return _highestMapId = new MapId(_highestMapId.Value + 1);
    }

    /// <inheritdoc />
    public event EventHandler<MapEventArgs>? MapCreated;

    /// <inheritdoc />
    public event EventHandler<MapEventArgs>? MapDestroyed;

    protected MapId CreateMap(MapId? mapId, EntityUid entityUid)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        var actualId = mapId ?? new MapId(_highestMapId.Value + 1);

        if (MapExists(actualId))
            throw new InvalidOperationException($"A map with ID {actualId} already exists");

        if (_highestMapId.Value < actualId.Value)
            _highestMapId = actualId;

        Logger.InfoS("map", $"Creating new map {actualId}");

        if (actualId != MapId.Nullspace) // nullspace isn't bound to an entity
        {
            var mapComps = EntityManager.EntityQuery<MapComponent>(true);

            IMapComponent? result = null;
            foreach (var mapComp in mapComps)
            {
                if (mapComp.WorldMap != actualId)
                    continue;

                result = mapComp;
                break;
            }

            if (result != null)
            {
                _mapEntities.Add(actualId, result.Owner);
                Logger.DebugS("map", $"Rebinding map {actualId} to entity {result.Owner}");
            }
            else
            {
                var newEnt = EntityManager.CreateEntityUninitialized(null, entityUid);
                _mapEntities.Add(actualId, newEnt);

                var mapComp = EntityManager.AddComponent<MapComponent>(newEnt);
                mapComp.WorldMap = actualId;
                EntityManager.InitializeComponents(newEnt);
                EntityManager.StartComponents(newEnt);
                Logger.DebugS("map", $"Binding map {actualId} to entity {newEnt}");
            }
        }
        else
        {
            _mapEntities.Add(MapId.Nullspace, EntityUid.Invalid);
        }

        var args = new MapEventArgs(actualId);
        OnMapCreatedGridTree(args);
        MapCreated?.Invoke(this, args);

        return actualId;
    }

    private void EnsureNullspaceExistsAndClear()
    {
        if (!MapExists(MapId.Nullspace))
            CreateMap(MapId.Nullspace);
        else
        {
            // nullspace ent may not have been allocated, but the mapId does exist, so we are done here
            if (!_mapEntities.TryGetValue(MapId.Nullspace, out var mapEntId) || mapEntId == EntityUid.Invalid)
                return;

            // the point of this is to completely clear the map without remaking the allocated entity, which would invalidate the
            // euid which I'm sure some code has cached. Notice this does not touch the map ent itself, so any comps added to it are not removed,
            // so i guess don't do that?
            foreach (var childTransform in EntityManager.GetComponent<TransformComponent>(mapEntId).Children.ToArray())
            {
                EntityManager.DeleteEntity(childTransform.Owner);
            }
        }
    }
}
