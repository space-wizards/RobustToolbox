using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Vector2 = System.Numerics.Vector2;

namespace Robust.Shared.EntitySerialization.Systems;

// This partial class file contains methods specific to loading maps
public sealed partial class MapLoaderSystem
{
    /// <summary>
    /// Attempts to load a file containing a single map.
    /// If the file does not contain exactly one map, this will return false and delete all loaded entities.
    /// </summary>
    /// <remarks>
    /// Note that this will not automatically initialize the map, unless specified via the <see cref="DeserializationOptions"/>.
    /// </remarks>
    public bool TryLoadMap(
        ResPath path,
        [NotNullWhen(true)] out Entity<MapComponent>? map,
        [NotNullWhen(true)] out HashSet<Entity<MapGridComponent>>? grids,
        DeserializationOptions? options = null,
        Vector2 offset = default,
        Angle rot = default)
    {
        var opts = new MapLoadOptions
        {
            Offset = offset,
            Rotation = rot,
            DeserializationOptions = options ?? DeserializationOptions.Default,
            ExpectedCategory = FileCategory.Map
        };

        map = null;
        grids = null;
        if (!TryLoadGeneric(path, out var result, opts))
            return false;

        if (result.Maps.Count == 1)
        {
            map = result.Maps.First();
            grids = result.Grids;
            return true;
        }

        Delete(result);
        return false;
    }

    /// <summary>
    /// Attempts to load a file containing a single map, assign it the given map id.
    /// </summary>
    /// <remarks>
    /// If possible, it is better to use <see cref="TryLoadMap"/> which automatically assigns a <see cref="MapId"/>.
    /// </remarks>
    /// <remarks>
    /// Note that this will not automatically initialize the map, unless specified via the <see cref="DeserializationOptions"/>.
    /// </remarks>
    public bool TryLoadMapWithId(
        MapId mapId,
        ResPath path,
        [NotNullWhen(true)] out Entity<MapComponent>? map,
        [NotNullWhen(true)] out HashSet<Entity<MapGridComponent>>? grids,
        DeserializationOptions? options = null,
        Vector2 offset = default,
        Angle rot = default)
    {
        map = null;
        grids = null;

        var opts = new MapLoadOptions
        {
            Offset = offset,
            Rotation = rot,
            DeserializationOptions = options ?? DeserializationOptions.Default,
            ExpectedCategory = FileCategory.Map
        };

        if (_mapSystem.MapExists(mapId))
            throw new Exception($"Target map already exists");

        opts.ForceMapId = mapId;
        if (!TryLoadGeneric(path, out var result, opts))
            return false;

        if (!_mapSystem.TryGetMap(mapId, out var uid) || !TryComp(uid, out MapComponent? comp))
            return false;

        map = new(uid.Value, comp);
        grids = result.Grids;
        return true;
    }

    /// <summary>
    /// Attempts to load a file containing a single map, and merge its children onto another map. After which the
    /// loaded map gets deleted.
    /// </summary>
    public bool TryMergeMap(
        MapId mapId,
        ResPath path,
        [NotNullWhen(true)] out HashSet<Entity<MapGridComponent>>? grids,
        DeserializationOptions? options = null,
        Vector2 offset = default,
        Angle rot = default)
    {
        grids = null;

        var opts = new MapLoadOptions
        {
            Offset = offset,
            Rotation = rot,
            DeserializationOptions = options ?? DeserializationOptions.Default,
            ExpectedCategory = FileCategory.Map
        };

        if (!_mapSystem.MapExists(mapId))
            throw new Exception($"Target map {mapId} does not exist");

        opts.MergeMap = mapId;
        if (!TryLoadGeneric(path, out var result, opts))
            return false;

        if (!_mapSystem.TryGetMap(mapId, out var uid) || !TryComp(uid, out MapComponent? comp))
            return false;

        grids = result.Grids;
        return true;
    }

    private void MergeMaps(EntityDeserializer deserializer, MapLoadOptions opts, HashSet<EntityUid> merged)
    {
        if (opts.MergeMap is not {} targetId)
            return;

        if (!_mapSystem.TryGetMap(targetId, out var targetUid))
            throw new Exception($"Target map {targetId} does not exist");

        deserializer.Result.Category = FileCategory.Unknown;
        var rotation = opts.Rotation;
        var matrix = Matrix3Helpers.CreateTransform(opts.Offset, rotation);
        var target = new Entity<TransformComponent>(targetUid.Value, Transform(targetUid.Value));

        // We want to apply the transforms to all children of any loaded maps. However, we can't just iterate over the
        // children of loaded maps, as transform component has not yet been initialized. I.e. xform.Children is empty.
        // Hence we iterate over all entities and check which ones are attached to maps.
        HashSet<EntityUid> maps = new();
        HashSet<EntityUid> logged = new();
        foreach (var uid in deserializer.Result.Entities)
        {
            var xform = Transform(uid);
            if (!_mapQuery.HasComp(xform.ParentUid))
                continue;

            if (_gridQuery.HasComponent(xform.ParentUid) && logged.Add(xform.ParentUid))
            {
                Log.Error($"Merging a grid-map onto another map is not supported.");
                continue;
            }

            maps.Add(xform.ParentUid);
            Merge(merged, uid, target, matrix, rotation);
        }

        deserializer.ToDelete.UnionWith(maps);
        deserializer.Result.Maps.RemoveWhere(x => maps.Contains(x.Owner));

        foreach (var uid in deserializer.Result.Orphans)
        {
            Merge(merged, uid, target, matrix, rotation);
        }

        deserializer.Result.Orphans.Clear();
    }

    private void Merge(
        HashSet<EntityUid> merged,
        EntityUid uid,
        Entity<TransformComponent> target,
        in Matrix3x2 matrix,
        Angle rotation)
    {
        merged.Add(uid);
        var xform = Transform(uid);
        var angle = xform.LocalRotation + rotation;
        var pos = Vector2.Transform(xform.LocalPosition, matrix);
        var coords = new EntityCoordinates(target.Owner, pos);
        _xform.SetCoordinates((uid, xform, MetaData(uid)), coords, rotation: angle, newParent: target.Comp);
    }

    private void MapInitalizeMerged(HashSet<EntityUid> merged, MapId targetId)
    {
        // fuck me I hate this map merging bullshit.
        // loading a map "onto" another map shouldn't need to be supported by the generic map loading methods.
        // If something needs to do that, it should implement it itself.
        // AFAIK this only exists for the loadgamemap command?

        if (!_mapSystem.TryGetMap(targetId, out var targetUid))
            throw new Exception($"Target map {targetId} does not exist");

        if (_mapSystem.IsInitialized(targetUid.Value))
        {
            foreach (var uid in merged)
            {
                _mapSystem.RecursiveMapInit(uid);
            }
        }

        var paused = _mapSystem.IsPaused(targetUid.Value);
        foreach (var uid in merged)
        {
            _mapSystem.RecursiveSetPaused(uid, paused);
        }
    }

    private bool SetMapId(EntityDeserializer deserializer, MapLoadOptions opts)
    {
        if (opts.ForceMapId is not { } id)
            return true;

        if (deserializer.Result.Maps.Count != 1)
        {
            Log.Error(
                $"The {nameof(MapLoadOptions.ForceMapId)} option is only supported when loading a file containing a single map.");
            Delete(deserializer.Result);
            return false;
        }

        var map = deserializer.Result.Maps.Single();
        _mapSystem.AssignMapId(map, id);
        return true;
    }
}
