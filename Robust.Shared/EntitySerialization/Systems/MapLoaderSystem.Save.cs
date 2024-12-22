using System;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.EntitySerialization.Systems;

public sealed partial class MapLoaderSystem
{
    /// <inheritdoc cref="EntitySerializer.OnIsSerializeable"/>
    public event EntitySerializer.IsSerializableDelegate? OnIsSerializable;

    /// <summary>
    /// Recursively serialize the given entity and its children.
    /// </summary>
    public (MappingDataNode Node, FileCategory Category) SerializeEntityRecursive(EntityUid uid, SerializationOptions? options = null)
    {
        _stopwatch.Restart();
        var opts = options ?? SerializationOptions.Default with
        {
            ExpectPreInit = (LifeStage(uid) < EntityLifeStage.MapInitialized)
        };

        var serializer = new EntitySerializer(_dependency, opts);
        serializer.OnIsSerializeable += OnIsSerializable;

        serializer.SerializeEntityRecursive(uid);

        var data = serializer.Write();
        var cat = serializer.GetCategory();

        Log.Debug($"Serialized {serializer.EntityData.Count} entities in {_stopwatch.Elapsed}");
        return (data, cat);
    }

    /// <summary>
    /// This method will serialize an entity and all of its children, and will write the result to a yaml file.
    /// If you are specifically trying to save a single grid or map, you should probably use SaveGrid or SaveMap instead.
    /// </summary>
    public FileCategory SaveEntity(EntityUid uid, ResPath path, SerializationOptions? options = null)
    {
        if (!Exists(uid))
            throw new Exception($"{uid} does not exist.");

        var ev = new BeforeSaveEvent(uid, Transform(uid).MapUid);
        RaiseLocalEvent(ev);

        Log.Debug($"Saving entity {ToPrettyString(uid)} to {path}");

        var data = SerializeEntityRecursive(uid, options);

        var ev2 = new AfterSaveEvent(uid, Transform(uid).MapUid, data.Node, data.Category);
        RaiseLocalEvent(ev2);

        var document = new YamlDocument(ev2.Node.ToYaml());
        var resPath = path.ToRootedPath();
        _resourceManager.UserData.CreateDir(resPath.Directory);

        using var writer = _resourceManager.UserData.OpenWriteText(resPath);
        {
            var stream = new YamlStream {document};
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        Log.Info($"Saved {ToPrettyString(uid)} to {path}");

        return data.Item2;
    }

    public void SaveMap(MapId id, string path, SerializationOptions? options = null)
        => SaveMap(id, new ResPath(path), options);

    public void SaveMap(MapId mapId, ResPath path, SerializationOptions? options = null)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
        {
            Log.Error($"Unable to find map {mapId}");
            return;
        }

        SaveMap(mapUid.Value, path, options);
    }

    public void SaveMap(EntityUid map, ResPath path, SerializationOptions? options = null)
    {
        if (!HasComp<MapComponent>(map))
        {
            Log.Error($"{ToPrettyString(map)} is not a map.");
            return;
        }

        var opts = options ?? SerializationOptions.Default;
        opts.Category = FileCategory.Map;

        var cat = SaveEntity(map, path, opts);
        if (cat != FileCategory.Map)
            Log.Error($"Failed to save {ToPrettyString(map)} as a map. Output: {cat}");
    }

    public void SaveGrid(EntityUid grid, string path, SerializationOptions? options = null)
        => SaveGrid(grid, new ResPath(path), options);

    public void SaveGrid(EntityUid grid, ResPath path, SerializationOptions? options = null)
    {
        if (!HasComp<MapGridComponent>(grid))
        {
            Log.Error($"{ToPrettyString(grid)} is not a grid.");
            return;
        }

        if (HasComp<MapComponent>(grid))
        {
            Log.Error($"{ToPrettyString(grid)} is a map, not (just) a grid.");
            return;
        }

        var opts = options ?? SerializationOptions.Default;
        opts.Category = FileCategory.Grid;

        var cat = SaveEntity(grid, path, opts);
        if (cat != FileCategory.Grid)
            Log.Error($"Failed to save {ToPrettyString(grid)} as a grid. Output: {cat}");
    }
}
