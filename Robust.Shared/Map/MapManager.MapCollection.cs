using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Timing;
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
    private readonly HashSet<MapId> _maps = new();
    private MapId _highestMapId = MapId.Nullspace;

    /// <inheritdoc />
    public virtual void DeleteMap(MapId mapId)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        if (!_maps.Contains(mapId))
            throw new InvalidOperationException($"Attempted to delete nonexistant map '{mapId}'");

        if (_mapEntities.TryGetValue(mapId, out var ent))
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
            _maps.Remove(mapId);
        }

        _mapEntities.Remove(mapId);

        Logger.InfoS("map", $"Deleting map {mapId}");
    }

    public MapId CreateMap(MapId? mapId = null)
    {
        return CreateMap(mapId, default);
    }

    /// <inheritdoc />
    public bool MapExists(MapId mapId)
    {
        return _maps.Contains(mapId);
    }

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

        if (!_maps.Contains(mapId))
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

    public bool HasMapEntity(MapId mapId)
    {
        return _mapEntities.ContainsKey(mapId);
    }

    public IEnumerable<MapId> GetAllMapIds()
    {
        return _maps;
    }

    public IEnumerable<IMapComponent> GetAllMapComponents()
    {
        return EntityManager.EntityQuery<IMapComponent>(true);
    }

    public bool IsMap(EntityUid uid)
    {
        return EntityManager.HasComponent<IMapComponent>(uid);
    }

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

        _maps.Add(actualId);
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

        var args = new MapEventArgs(actualId);
        OnMapCreatedGridTree(args);
        MapCreated?.Invoke(this, args);

        return actualId;
    }

    private void EnsureNullspaceExistsAndClear()
    {
        if (!_maps.Contains(MapId.Nullspace))
            CreateMap(MapId.Nullspace);
        else
        {
            if (!_mapEntities.TryGetValue(MapId.Nullspace, out var mapEntId))
                return;

            // Notice we do not clear off any added comps to the nullspace map entity, would it be better to just delete
            // and recreate the entity, letting recursive entity deletion perform this foreach loop?

            foreach (var childEuid in EntityManager.GetComponent<TransformComponent>(mapEntId).ChildEntities)
            {
                EntityManager.DeleteEntity(childEuid);
            }
        }
    }

    private void DeleteAllMaps()
    {
        foreach (var map in _maps.ToArray())
        {
            if (map != MapId.Nullspace)
                DeleteMap(map);
        }

        if (_mapEntities.TryGetValue(MapId.Nullspace, out var entId))
        {
            Logger.InfoS("map", $"Deleting map entity {entId}");
            EntityManager.DeleteEntity(entId);

            if (_mapEntities.Remove(MapId.Nullspace))
                Logger.InfoS("map", "Removing nullspace map entity.");
        }
    }
}
