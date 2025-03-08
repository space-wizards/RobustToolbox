using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Robust.Shared.EntitySerialization.Systems;

// This partial class file contains methods for serializing and saving entities, grids, and maps.
public sealed partial class MapLoaderSystem
{
    /// <inheritdoc cref="EntitySerializer.OnIsSerializeable"/>
    public event EntitySerializer.IsSerializableDelegate? OnIsSerializable;

    /// <summary>
    /// Recursively serialize the given entity and its children.
    /// </summary>
    public (MappingDataNode Node, FileCategory Category) SerializeEntitiesRecursive(
        HashSet<EntityUid> entities,
        SerializationOptions? options = null)
    {
        _stopwatch.Restart();
        if (!entities.All(Exists))
            throw new Exception($"Cannot serialize deleted entities");

        Log.Info($"Serializing entities: {string.Join(", ", entities.Select(x => ToPrettyString(x).ToString()))}");

        var maps = entities.Select(x => Transform(x).MapID).ToHashSet();
        var ev = new BeforeSerializationEvent(entities, maps);
        RaiseLocalEvent(ev);

        // In case no options were provided, we assume that if all of the starting entities are pre-init, we should
        // expect that **all** entities that get serialized should be pre-init.
        var opts = options ?? SerializationOptions.Default with
        {
            ExpectPreInit = (entities.All(x => LifeStage(x) < EntityLifeStage.MapInitialized))
        };

        var serializer = new EntitySerializer(_dependency, opts);
        serializer.OnIsSerializeable += OnIsSerializable;

        foreach (var ent in entities)
        {
            serializer.SerializeEntityRecursive(ent);
        }

        var data = serializer.Write();
        var cat = serializer.GetCategory();

        var ev2 = new AfterSerializationEvent(entities, data, cat);
        RaiseLocalEvent(ev2);

        Log.Debug($"Serialized {serializer.EntityData.Count} entities in {_stopwatch.Elapsed}");
        return (data, cat);
    }

    /// <summary>
    /// Serialize a standard (non-grid, non-map) entity and all of its children and write the result to a
    /// yaml file.
    /// </summary>
    public bool TrySaveEntity(EntityUid entity, ResPath path, SerializationOptions? options = null)
    {
        if (_mapQuery.HasComp(entity))
        {
            Log.Error($"{ToPrettyString(entity)} is a map. Use {nameof(TrySaveMap)}.");
            return false;
        }

        if (_gridQuery.HasComp(entity))
        {
            Log.Error($"{ToPrettyString(entity)} is a grid. Use {nameof(TrySaveGrid)}.");
            return false;
        }

        var opts = options ?? SerializationOptions.Default;
        opts.Category = FileCategory.Entity;

        MappingDataNode data;
        FileCategory cat;
        try
        {
            (data, cat) = SerializeEntitiesRecursive([entity], opts);
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while trying to serialize entity {ToPrettyString(entity)}:\n{e}");
            return false;
        }

        if (cat != FileCategory.Entity)
        {
            Log.Error($"Failed to save {ToPrettyString(entity)} as a singular entity. Output: {cat}");
            return false;
        }

        Write(path, data);
        return true;
    }

    /// <summary>
    /// Serialize a map and all of its children and write the result to a yaml file.
    /// </summary>
    public bool TrySaveMap(MapId mapId, ResPath path, SerializationOptions? options = null)
    {
        if (_mapSystem.TryGetMap(mapId, out var mapUid))
            return TrySaveMap(mapUid.Value, path, options);

        Log.Error($"Unable to find map {mapId}");
        return false;
    }

    /// <summary>
    /// Serialize a map and all of its children and write the result to a yaml file.
    /// </summary>
    public bool TrySaveMap(EntityUid map, ResPath path, SerializationOptions? options = null)
    {
        if (!_mapQuery.HasComp(map))
        {
            Log.Error($"{ToPrettyString(map)} is not a map.");
            return false;
        }

        var opts = options ?? SerializationOptions.Default;
        opts.Category = FileCategory.Map;

        MappingDataNode data;
        FileCategory cat;
        try
        {
            (data, cat) = SerializeEntitiesRecursive([map], opts);
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while trying to serialize map {ToPrettyString(map)}:\n{e}");
            return false;
        }

        if (cat != FileCategory.Map)
        {
            Log.Error($"Failed to save {ToPrettyString(map)} as a map. Output: {cat}");
            return false;
        }

        Write(path, data);
        return true;
    }

    /// <summary>
    /// Serialize a grid and all of its children and write the result to a yaml file.
    /// </summary>
    public bool TrySaveGrid(EntityUid grid, ResPath path, SerializationOptions? options = null)
    {
        if (!_gridQuery.HasComp(grid))
        {
            Log.Error($"{ToPrettyString(grid)} is not a grid.");
            return false;
        }

        if (_mapQuery.HasComp(grid))
        {
            Log.Error($"{ToPrettyString(grid)} is a map, not (just) a grid. Use {nameof(TrySaveMap)}");
            return false;
        }

        var opts = options ?? SerializationOptions.Default;
        opts.Category = FileCategory.Grid;

        MappingDataNode data;
        FileCategory cat;
        try
        {
            (data, cat) = SerializeEntitiesRecursive([grid], opts);
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while trying to serialize grid {ToPrettyString(grid)}:\n{e}");
            return false;
        }

        if (cat != FileCategory.Grid)
        {
            Log.Error($"Failed to save {ToPrettyString(grid)} as a grid. Output: {cat}");
            return false;
        }

        Write(path, data);
        return true;
    }

    /// <summary>
    /// Serialize an entities and all of their children to a yaml file.
    /// This makes no assumptions about the expected entity or resulting file category.
    /// If possible, use the map/grid specific variants instead.
    /// </summary>
    public bool TrySaveGeneric(
        EntityUid uid,
        ResPath path,
        out FileCategory category,
        SerializationOptions? options = null)
    {
        return TrySaveGeneric([uid], path, out category, options);
    }

    /// <summary>
    /// Serialize one or more entities and all of their children to a yaml file.
    /// This makes no assumptions about the expected entity or resulting file category.
    /// If possible, use the map/grid specific variants instead.
    /// </summary>
    public bool TrySaveGeneric(
        HashSet<EntityUid> entities,
        ResPath path,
        out FileCategory category,
        SerializationOptions? options = null)
    {
        category = FileCategory.Unknown;
        if (entities.Count == 0)
            return false;

        var opts = options ?? SerializationOptions.Default;

        MappingDataNode data;
        try
        {
            (data, category) = SerializeEntitiesRecursive(entities, opts);
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while trying to serialize entities:\n{e}");
            return false;
        }

        Write(path, data);
        return true;
    }
}
