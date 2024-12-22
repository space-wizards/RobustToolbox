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

public sealed partial class MapLoaderSystem
{
    /// <summary>
    /// Tries to load entities from a yaml file. If you are specifically trying to load a map or grid, you should
    /// probably be using <see cref="TryLoadMap"/> or <see cref="TryLoadGrid"/> instead
    /// </summary>
    /// <param name="file">The file to load.</param>
    /// <param name="result">Data class containing information about the loaded entities</param>
    /// <param name="options">Optional Options for configuring loading behaviour.</param>
    public bool TryLoadEntities(ResPath file, [NotNullWhen(true)] out LoadResult? result, MapLoadOptions? options = null)
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
            Log.Error($"Caught exception while creating entities: {e}");
            Delete(deserializer.Result);
            throw;
        }

        if (opts.ExpectedCategory is { } exp && exp != deserializer.Result.Category)
        {
            // Did someone try to load a map file as a grid or vice versa?
            Log.Error($"File does not contain the expected data. Expected {exp} but got {deserializer.Result.Category}");
            Delete(deserializer.Result);
            return false;
        }

        // Reparent entities if loading entities onto an existing map.
        var reparented = new HashSet<EntityUid>();
        MergeMaps(deserializer, opts, reparented);

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
            MapinitalizeReparented(reparented, map);

        result = deserializer.Result;
        Log.Debug($"Loaded map in {_stopwatch.Elapsed}");
        return true;
    }

    private bool SetMapId(EntityDeserializer deserializer, MapLoadOptions opts)
    {
        if (opts.ForceMapId is not {} id)
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

    /// <summary>
    /// Tries to load a file and return a list of grids and maps
    /// </summary>
    public bool TryLoadEntities(
        ResPath file,
        [NotNullWhen(true)] out HashSet<Entity<MapComponent>>? maps,
        [NotNullWhen(true)] out HashSet<Entity<MapGridComponent>>? grids,
        MapLoadOptions? options = null)
    {
        grids = null;
        maps = null;
        if (!TryLoadEntities(file, out var data, options))
            return false;

        maps = data.Maps;
        grids = data.Grids;
        return true;
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
        if (!TryLoadEntities(path, out var result, opts))
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
    /// Attempts to load a file containing a single map, and merge its children onto another map. After which the
    /// loaded map gets deleted.
    /// </summary>
    public bool TryMergeMap(
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

        if (!_mapSystem.MapExists(mapId))
            throw new Exception($"Target map {mapId} does not exist");

        opts.MergeMap = mapId;
        if (!TryLoadEntities(path, out var result, opts))
            return false;

        if (!_mapSystem.TryGetMap(mapId, out var uid) || !TryComp(uid, out MapComponent? comp))
            return false;

        map = new(uid.Value, comp);
        grids = result.Grids;
        return true;
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
        if (!TryLoadEntities(path, out var result, opts))
            return false;

        if (!_mapSystem.TryGetMap(mapId, out var uid) || !TryComp(uid, out MapComponent? comp))
            return false;

        map = new(uid.Value, comp);
        grids = result.Grids;
        return true;
    }

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
        if (!TryLoadEntities(path, out var result, opts))
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
    /// Helper method for deleting all loaded entities.
    /// </summary>
    public void Delete(LoadResult result)
    {
        foreach (var uid in result.Maps)
        {
            Del(uid);
        }

        foreach (var uid in result.Orphans)
        {
            Del(uid);
        }

        foreach (var uid in result.Entities)
        {
            Del(uid);
        }
    }

    private bool TryGetReader(ResPath resPath, [NotNullWhen(true)] out TextReader? reader)
    {
        if (_resourceManager.UserData.Exists(resPath))
        {
            // Log warning if file exists in both user and content data.
            if (_resourceManager.ContentFileExists(resPath))
                Log.Warning("Reading map user data instead of content");

            reader = _resourceManager.UserData.OpenText(resPath);
            return true;
        }

        if (_resourceManager.TryContentFileRead(resPath, out var contentReader))
        {
            reader = new StreamReader(contentReader);
            return true;
        }

        Log.Error($"File not found: {resPath}");
        reader = null;
        return false;
    }

    public bool TryReadFile(ResPath file, [NotNullWhen(true)] out MappingDataNode? data)
    {
        var resPath = file.ToRootedPath();
        data = null;

        if (!TryGetReader(resPath, out var reader))
            return false;

        Log.Info($"Loading file: {resPath}");
        _stopwatch.Restart();

        using var textReader = reader;
        var documents = DataNodeParser.ParseYamlStream(reader).ToArray();
        Log.Debug($"Loaded yml stream in {_stopwatch.Elapsed}");

        // Yes, logging errors in a "try" method is kinda shit, but it was throwing exceptions when I found it and it does
        // make sense to at least provide some kind of feedback for why it failed.
        switch (documents.Length)
        {
            case < 1:
                Log.Error("Stream has no YAML documents.");
                return false;
            case > 1:
                Log.Error("Stream too many YAML documents. Map files store exactly one.");
                return false;
            default:
                data = (MappingDataNode) documents[0].Root;
                return true;
        }
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

    private void MapinitalizeReparented(HashSet<EntityUid> reparented, MapId targetId)
    {
        // fuck me I hate this map merging bullshit.
        // loading a map "onto" another map shouldn't need to be supported by the generic map loading methods.
        // If something needs to do that, it should implement it itself.
        // AFAIK this only exists for the loadgamemap command?

        if (!_mapSystem.TryGetMap(targetId, out var targetUid))
            throw new Exception($"Target map {targetId} does not exist");

        if (_mapSystem.IsInitialized(targetUid.Value))
        {
            foreach (var uid in reparented)
            {
                _mapSystem.RecursiveMapInit(uid);
            }
        }

        var paused = _mapSystem.IsPaused(targetUid.Value);
        foreach (var uid in reparented)
        {
            _mapSystem.RecursiveSetPaused(uid, paused);
        }
    }

    private void MergeMaps(EntityDeserializer deserializer, MapLoadOptions opts, HashSet<EntityUid> reparented)
    {
        if (opts.MergeMap is not {} targetId)
            return;

        if (!_mapSystem.TryGetMap(targetId, out var targetUid))
            throw new Exception($"Target map {targetId} does not exist");

        deserializer.Result.Category = FileCategory.Unknown;
        var rotation = opts.Rotation;
        var matrix = Matrix3Helpers.CreateTransform(opts.Offset, rotation);
        var target = new Entity<TransformComponent>(targetUid.Value, Transform(targetUid.Value));

        foreach (var uid in deserializer.Result.Orphans)
        {
            Reparent(reparented, uid, target, matrix, rotation);
        }

        deserializer.Result.Orphans.Clear();

        foreach (var uid in deserializer.Result.Entities)
        {
            var xform = Transform(uid);
            if (!_mapQuery.HasComp(xform.ParentUid))
                continue;

            // The original comment around this bit of logic was just:
            // > Smelly
            // I don't know what sloth meant by that, but I guess loading a grid-map onto another grid-map for whatever
            // reason must be done without offsets?
            // Or more generally, loading a mapgrid onto another (potentially non-mapgrid) map is just generally kind of weird.
            if (_gridQuery.HasComponent(xform.ParentUid))
            {
                Reparent(reparented, uid, target, Matrix3x2.Identity, Angle.Zero);
            }
            else
            {
                Reparent(reparented, uid, target, matrix, rotation);
            }
        }

        deserializer.ToDelete.UnionWith(deserializer.Result.Maps.Select(x => x.Owner));
        deserializer.Result.Maps.Clear();
    }

    private void Reparent(
        HashSet<EntityUid> reparented,
        EntityUid uid,
        Entity<TransformComponent> target,
        in Matrix3x2 matrix,
        Angle rotation)
    {
        reparented.Add(uid);
        var xform = Transform(uid);
        var angle = xform.LocalRotation + rotation;
        var pos = Vector2.Transform(xform.LocalPosition, matrix);
        var coords = new EntityCoordinates(target.Owner, pos);
        _xform.SetCoordinates((uid, xform, MetaData(uid)), coords, rotation: angle, newParent: target.Comp);
    }
}
