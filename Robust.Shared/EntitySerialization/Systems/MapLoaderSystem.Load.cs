using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using Vector2 = System.Numerics.Vector2;

namespace Robust.Shared.EntitySerialization.Systems;

// This partial class file contains methods for loading generic entities and grids. Map specific methods are in another
// file
public sealed partial class MapLoaderSystem
{
    /// <summary>
    /// Tries to load entities from a yaml file. Whenever possible, you should try to use <see cref="TryLoadMap"/>,
    /// <see cref="TryLoadGrid"/>, or <see cref="TryLoadEntity"/> instead.
    /// </summary>
    public bool TryLoadGeneric(
        ResPath file,
        [NotNullWhen(true)] out HashSet<Entity<MapComponent>>? maps,
        [NotNullWhen(true)] out HashSet<Entity<MapGridComponent>>? grids,
        MapLoadOptions? options = null)
    {
        grids = null;
        maps = null;
        if (!TryLoadGeneric(file, out var data, options))
            return false;

        maps = data.Maps;
        grids = data.Grids;
        return true;
    }

    /// <summary>
    /// Tries to load entities from a yaml file. Whenever possible, you should try to use <see cref="TryLoadMap"/>,
    /// <see cref="TryLoadGrid"/>, or <see cref="TryLoadEntity"/> instead.
    /// </summary>
    /// <param name="file">The file to load.</param>
    /// <param name="result">Data class containing information about the loaded entities</param>
    /// <param name="options">Optional Options for configuring loading behaviour.</param>
    public bool TryLoadGeneric(ResPath file, [NotNullWhen(true)] out LoadResult? result, MapLoadOptions? options = null)
    {
        result = null;

        if (!TryReadFile(file, out var data))
            return false;

        _stopwatch.Restart();
        var ev = new BeforeEntityReadEvent();
        RaiseLocalEvent(ev);

        var opts = options ?? MapLoadOptions.Default;

        // If we are forcing a map id, we cannot auto-assign ids.
        opts.DeserializationOptions.AssignMapids = opts.ForceMapId == null;

        if (opts.MergeMap is { } targetId && !_mapSystem.MapExists(targetId))
            throw new Exception($"Target map {targetId} does not exist");

        if (opts.MergeMap != null && opts.ForceMapId != null)
            throw new Exception($"Invalid combination of MapLoadOptions");

        if (_mapSystem.MapExists(opts.ForceMapId))
            throw new Exception($"Target map already exists");

        // Using a local deserializer instead of a cached value, both to ensure that we don't accidentally carry over
        // data from a previous serializations, and because some entities cause other maps/grids to be loaded during
        // during mapinit.
        var deserializer = new EntityDeserializer(
            _dependency,
            data,
            opts.DeserializationOptions,
            ev.RenamedPrototypes,
            ev.DeletedPrototypes);

        if (!deserializer.TryProcessData())
        {
            Log.Debug($"Failed to process entity data in {file}");
            return false;
        }

        try
        {
            deserializer.CreateEntities();
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while creating entities for map {file}: {e}");
            Delete(deserializer.Result);
            throw;
        }

        if (opts.ExpectedCategory is { } exp && exp != deserializer.Result.Category)
        {
            // Did someone try to load a map file as a grid or vice versa?
            Log.Error($"Map {file} does not contain the expected data. Expected {exp} but got {deserializer.Result.Category}");
            Delete(deserializer.Result);
            return false;
        }

        // Reparent entities if loading entities onto an existing map.
        var merged = new HashSet<EntityUid>();
        MergeMaps(deserializer, opts, merged);

        if (!SetMapId(deserializer, opts))
            return false;

        // Apply any offsets & rotations specified by the load options
        ApplyTransform(deserializer, opts);

        try
        {
            deserializer.StartEntities();
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while starting entities: {e}");
            Delete(deserializer.Result);
            throw;
        }

        if (opts.MergeMap is {} map)
            MapInitalizeMerged(merged, map);

        result = deserializer.Result;
        Log.Debug($"Loaded map in {_stopwatch.Elapsed}");
        return true;
    }

    /// <summary>
    /// Tries to load a regular (non-map, non-grid) entity from a file.
    /// The loaded entity will initially be in null-space.
    /// If the file does not contain exactly one orphaned entity, this will return false and delete loaded entities.
    /// </summary>
    public bool TryLoadEntity(
        ResPath path,
        [NotNullWhen(true)] out Entity<TransformComponent>? entity,
        DeserializationOptions? options = null)
    {
        var opts = new MapLoadOptions
        {
            DeserializationOptions = options ?? DeserializationOptions.Default,
            ExpectedCategory = FileCategory.Entity
        };

        entity = null;
        if (!TryLoadGeneric(path, out var result, opts))
            return false;

        if (result.Orphans.Count == 1)
        {
            var uid = result.Orphans.Single();
            entity = (uid, Transform(uid));
            return true;
        }

        Delete(result);
        return false;
    }

    /// <summary>
    /// Tries to load a grid entity from a file and parent it to the given map.
    /// If the file does not contain exactly one grid, this will return false and delete loaded entities.
    /// </summary>
    public bool TryLoadGrid(
        MapId map,
        ResPath path,
        [NotNullWhen(true)] out Entity<MapGridComponent>? grid,
        DeserializationOptions? options = null,
        Vector2 offset = default,
        Angle rot = default)
    {
        var opts = new MapLoadOptions
        {
            MergeMap = map,
            Offset = offset,
            Rotation = rot,
            DeserializationOptions = options ?? DeserializationOptions.Default,
            ExpectedCategory = FileCategory.Grid
        };

        grid = null;
        if (!TryLoadGeneric(path, out var result, opts))
            return false;

        if (result.Grids.Count == 1)
        {
            grid = result.Grids.Single();
            return true;
        }

        Delete(result);
        return false;
    }

    /// <summary>
    /// Tries to load a grid entity from a file and parent it to a newly created map.
    /// If the file does not contain exactly one grid, this will return false and delete loaded entities.
    /// </summary>
    public bool TryLoadGrid(
        ResPath path,
        [NotNullWhen(true)] out Entity<MapComponent>? map,
        [NotNullWhen(true)] out Entity<MapGridComponent>? grid,
        DeserializationOptions? options = null,
        Vector2 offset = default,
        Angle rot = default)
    {
        var opts = options ?? DeserializationOptions.Default;

        var mapUid = _mapSystem.CreateMap(out var mapId, runMapInit: opts.InitializeMaps);
        if (opts.PauseMaps)
            _mapSystem.SetPaused(mapUid, true);

        if (!TryLoadGrid(mapId, path, out grid, options, offset, rot))
        {
            Del(mapUid);
            map = null;
            return false;
        }

        map = new(mapUid, Comp<MapComponent>(mapUid));
        return true;
    }

    private void ApplyTransform(EntityDeserializer deserializer, MapLoadOptions opts)
    {
        if (opts.Rotation == Angle.Zero && opts.Offset == Vector2.Zero)
            return;

        // If merging onto a single map, the transformation was already applied by SwapRootNode()
        if (opts.MergeMap != null)
            return;

        var matrix = Matrix3Helpers.CreateTransform(opts.Offset, opts.Rotation);

        // We want to apply the transforms to all children of any loaded maps. However, we can't just iterate over the
        // children of loaded maps, as transform component has not yet been initialized. I.e. xform.Children is empty.
        // Hence we iterate over all entities and check which ones are attached to maps.
        foreach (var uid in deserializer.Result.Entities)
        {
            var xform = Transform(uid);

            if (!_mapQuery.HasComp(xform.ParentUid))
                continue;

            // The original comment around this bit of logic was just:
            // > Smelly
            // I don't know what sloth meant by that, but I guess applying transforms to grid-maps is a no-no?
            // Or more generally, loading a mapgrid onto another (potentially non-mapgrid) map is just generally kind of weird.
            if (_gridQuery.HasComponent(xform.ParentUid))
                continue;

            var rot = xform.LocalRotation + opts.Rotation;
            var pos = Vector2.Transform(xform.LocalPosition, matrix);
            _xform.SetLocalPositionRotation(uid, pos, rot, xform);
            DebugTools.Assert(!xform.NoLocalRotation || xform.LocalRotation == 0);
        }
    }
}
