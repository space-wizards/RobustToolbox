using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.EntitySerialization;

/// <summary>
/// This class provides methods for deserializing entities from yaml. It provides some more control over
/// serialization than the methods provided by <see cref="MapLoaderSystem"/>.
/// </summary>
public sealed class EntityDeserializer : ISerializationContext, IEntityLoadContext,
    ITypeSerializer<EntityUid, ValueDataNode>
{
    public const int OldestSupportedVersion = 3;
    public const int NewestSupportedVersion = EntitySerializer.MapFormatVersion;

    public SerializationManager.SerializerProvider SerializerProvider { get; } = new();

    [Dependency] public readonly EntityManager EntMan = default!;
    [Dependency] public readonly IGameTiming Timing = default!;
    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    private readonly ISawmill _log;
    private Stopwatch _stopwatch = new();

    public readonly DeserializationOptions Options;

    /// <summary>
    /// Serialized entity data that is going to be read.
    /// </summary>
    public readonly MappingDataNode Data;

    /// <summary>
    /// Subset of the file's <see cref="Data"/> relevant to each entity, indexed by their allocated EntityUids
    /// </summary>
    public readonly Dictionary<EntityUid, EntData> Entities = new();

    /// <summary>
    /// Variant of <see cref="Entities"/> indexed by the entity's yaml id.
    /// </summary>
    public readonly Dictionary<int, EntData> YamlEntities = new();

    /// <summary>
    /// Entity data grouped by their entity prototype id. Any entities without a prototype or with an invalid or
    /// deleted prototypes use an empty string.
    /// </summary>
    public readonly Dictionary<string, List<EntData>> Prototypes = new();

    public readonly record struct EntData(int YamlId, MappingDataNode Node, bool PostInit, bool Paused, bool ToDelete);

    public readonly LoadResult Result = new();
    public readonly Dictionary<int, string> TileMap = new();
    public readonly Dictionary<int, EntityUid> UidMap = new();
    public readonly List<int> MapYamlIds = new();
    public readonly List<int> GridYamlIds = new();
    public readonly List<int> OrphanYamlIds = new();
    public readonly Dictionary<string, string> RenamedPrototypes;
    public readonly HashSet<string> DeletedPrototypes;

    /// <summary>
    /// Entities that need to be flagged as map-initialized. This will not actually run map-init logic, this is for
    /// loading entities that have already been map-initialized and just need to be flagged as such.
    /// </summary>
    public readonly HashSet<EntityUid> PostMapInit = new();
    public readonly HashSet<EntityUid> Paused = new();
    public readonly HashSet<EntityUid> ToDelete = new();
    public readonly List<EntityUid> SortedEntities = new();

    public readonly Dictionary<string, IComponent> CurrentReadingEntityComponents = new();
    public EntData? CurrentReadingEntity;
    public HashSet<string> CurrentlyIgnoredComponents = new();
    public string? CurrentComponent;
    private readonly EntityQuery<MapComponent> _mapQuery;
    private readonly EntityQuery<MapGridComponent> _gridQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<MetaDataComponent> _metaQuery;

    public EntityDeserializer(
        IDependencyCollection deps,
        MappingDataNode data,
        DeserializationOptions options,
        Dictionary<string, string>? renamedPrototypes = null,
        HashSet<string>? deletedPrototypes = null)
    {
        deps.InjectDependencies(this);
        _log = _logMan.GetSawmill("entity_deserializer");
        _log.Level = LogLevel.Info;
        SerializerProvider.RegisterSerializer(this);
        Data = data;
        Options = options;
        RenamedPrototypes = renamedPrototypes ?? new();
        DeletedPrototypes = deletedPrototypes ?? new();

        _mapQuery = EntMan.GetEntityQuery<MapComponent>();
        _gridQuery = EntMan.GetEntityQuery<MapGridComponent>();
        _xformQuery = EntMan.GetEntityQuery<TransformComponent>();
        _metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
    }

    /// <summary>
    /// Are we currently iterating prototypes or entities for writing.
    /// This is used to suppress some serialization errors/warnings.
    /// </summary>
    public bool WritingReadingPrototypes { get; private set; }

    /// <summary>
    /// This processes some of the data in <see cref="Data"/>, including extracting the metadata, tile-map,
    /// validating that all referenced entity prototypes exists, and generating collections for accessing entity data.
    /// </summary>
    /// <returns>Returns false if the entity data cannot be processed</returns>
    public bool TryProcessData()
    {
        ReadMetadata();

        if (Result.Version < OldestSupportedVersion)
        {
            _log.Error(
                $"Cannot handle this map file version, found v{Result.Version} and require at least v{OldestSupportedVersion}");
            return false;
        }

        if (Result.Version > NewestSupportedVersion)
        {
            _log.Error(
                $"Cannot handle this map file version, found v{Result.Version} but require at most v{NewestSupportedVersion}");
            return false;
        }

        if (!ValidatePrototypes())
            return false;

        ReadEntities();
        ReadTileMap();
        ReadMapsAndGrids();
        return true;
    }

    /// <summary>
    /// Allocate entities, load the per-entity serialized data, and populate the various entity collections.
    /// </summary>
    public void CreateEntities()
    {
        // Alloc entities, and populate the yaml uid -> EntityUid maps
        AllocateEntities();

        // Load the prototype data onto entities, e.g. transform parents, etc.
        LoadEntities();

        // Get the lists of maps, grids, and orphan entities
        GetMapsAndGrids();

        // grids prior to engine v175 might've been serialized with empty chunks which now throw debug asserts.
        RemoveEmptyChunks();

        // Assign MapSaveTileMapComponent to all read grids. This is used to avoid large file diffs if the tile map changes.
        StoreGridTileMap();

        // Separate maps and orphaned entities out from the "true" null-space entities.
        ProcessNullspaceEntities();

        if (Options.AssignMapids)
            AssignMapIds();

        CheckCategory();
    }

    /// <summary>
    /// Finish entity startup & initialization, and delete any invalid entities
    /// </summary>
    public void StartEntities()
    {
        AdoptGrids();
        ValidateMapIds();
        BuildEntityHierarchy();
        StartEntitiesInternal();

        // Set loaded entity metadata
        SetMapInitLifestage();
        SetPaused();

        // Apply entity metadata options
        PauseMaps();
        InitializeMaps();

        ProcessDeletions();
    }

    private void ReadMetadata()
    {
        var meta = Data.Get<MappingDataNode>("meta");
        Result.Version = meta.Get<ValueDataNode>("format").AsInt();

        if (meta.TryGet<ValueDataNode>("engineVersion", out var engVer))
            Result.EngineVersion = engVer.Value;

        if (meta.TryGet<ValueDataNode>("forkId", out var forkId))
            Result.ForkId = forkId.Value;

        if (meta.TryGet<ValueDataNode>("forkVersion", out var forkVer))
            Result.ForkVersion = forkVer.Value;

        if (meta.TryGet<ValueDataNode>("time", out var timeNode) && DateTime.TryParse(timeNode.Value, out var time))
            Result.Time = time;

        if (meta.TryGet<ValueDataNode>("category", out var catNode) &&
            Enum.TryParse<FileCategory>(catNode.Value, out var res))
        {
            Result.Category = res;
        }
    }

    /// <summary>
    /// Verify that the entity prototypes referenced in the file are all valid.
    /// </summary>
    private bool ValidatePrototypes()
    {
        _stopwatch.Restart();
        var fail = false;
        var key = Result.Version >= 4 ? "proto" : "type";
        var entities = Data.Get<SequenceDataNode>("entities");

        foreach (var metaDef in entities.Cast<MappingDataNode>())
        {
            if (!metaDef.TryGet<ValueDataNode>(key, out var typeNode))
                continue;

            var type = typeNode.Value;
            if (string.IsNullOrWhiteSpace(type))
                continue;

            if (RenamedPrototypes.TryGetValue(type, out var newType))
                type = newType;

            if (DeletedPrototypes.Contains(type))
            {
                _log.Warning("Map contains an obsolete/removed prototype: {0}. This may cause unexpected errors.", type);
                continue;
            }

            if (_proto.HasIndex<EntityPrototype>(type))
                continue;

            _log.Error("Missing prototype for map: {0}", type);
            fail = true;
        }

        _log.Debug($"Verified entities in {_stopwatch.Elapsed}");

        if (!fail)
            return true;

        _log.Error("Found missing prototypes in map file. Missing prototypes have been dumped to logs.");
        return false;
    }

    /// <summary>
    /// Read entity section and populate <see cref="Prototypes"/> groups. This does not actually create entities, it just
    /// groups them by their prototype.
    /// </summary>
    private void ReadEntities()
    {
        if (Result.Version == 3)
        {
            ReadEntitiesV3();
            return;
        }

        if (Result.Version < 7)
        {
            // Older versions do not have per-entity mapinit and paused information.
            // But otherwise mostly identical
            ReadEntitiesFallback();
            return;
        }

        // entities are grouped by prototype.
        var prototypeGroups = Data.Get<SequenceDataNode>("entities");
        foreach (var protoGroup in prototypeGroups.Cast<MappingDataNode>())
        {
            EntProtoId? protoId = null;
            var deletedPrototype = false;
            if (protoGroup.TryGet<ValueDataNode>("proto", out var protoIdNode)
                && !string.IsNullOrWhiteSpace(protoIdNode.Value))
            {
                if (DeletedPrototypes.Contains(protoIdNode.Value))
                {
                    deletedPrototype = true;
                    if (_proto.HasIndex<EntityPrototype>(protoIdNode.Value))
                        protoId = protoIdNode.Value;
                }
                else if (RenamedPrototypes.TryGetValue(protoIdNode.Value, out var newType))
                    protoId = newType;
                else
                    protoId = protoIdNode.Value;
            }

            var entities = (SequenceDataNode) protoGroup["entities"];
            _proto.TryIndex(protoId, out var proto);

            var protoData = Prototypes.GetOrNew(proto?.ID ?? string.Empty);
            foreach (var entityNode in entities.Cast<MappingDataNode>())
            {
                var yamlId = entityNode.Get<ValueDataNode>("uid").AsInt();
                var postInit = entityNode.TryGet<ValueDataNode>("mapInit", out var initNode) && initNode.AsBool();

                // If the paused field does not exist, the default value depends on whether or not the entity has been
                // map-initialized.
                var paused = entityNode.TryGet<ValueDataNode>("paused", out var pausedNode)
                    ? pausedNode.AsBool()
                    : !postInit;

                var entData = new EntData(yamlId, entityNode, postInit, paused, deletedPrototype);
                protoData.Add(entData);
                YamlEntities.Add(yamlId, entData);
            }
        }
    }

    private void ReadEntitiesV3()
    {
        var metadata = Data.Get<MappingDataNode>("meta");
        var preInit = metadata.TryGet<ValueDataNode>("postmapinit", out var mapInitNode) && !mapInitNode.AsBool();

        var entities = Data.Get<SequenceDataNode>("entities");
        foreach (var entityNode in entities.Cast<MappingDataNode>())
        {
            var yamlId = entityNode.Get<ValueDataNode>("uid").AsInt();
            EntProtoId? protoId = null;
            var toDelete = false;
            if (entityNode.TryGet<ValueDataNode>("type", out var protoIdNode))
            {
                if (DeletedPrototypes.Contains(protoIdNode.Value))
                {
                    toDelete = true;
                    if (_proto.HasIndex<EntityPrototype>(protoIdNode.Value))
                        protoId = protoIdNode.Value;
                }
                else if (RenamedPrototypes.TryGetValue(protoIdNode.Value, out var newType))
                    protoId = newType;
                else
                    protoId = protoIdNode.Value;
            }

            _proto.TryIndex(protoId, out var proto);
            var protoData = Prototypes.GetOrNew(proto?.ID ?? string.Empty);
            var entData = new EntData(yamlId, entityNode, PostInit: !preInit, Paused: preInit, toDelete);
            protoData.Add(entData);
            YamlEntities.Add(yamlId, entData);
        }
    }

    private void ReadEntitiesFallback()
    {
        var metadata = Data.Get<MappingDataNode>("meta");
        var preInit = metadata.TryGet<ValueDataNode>("postmapinit", out var mapInitNode) && !mapInitNode.AsBool();

        var prototypeGroups = Data.Get<SequenceDataNode>("entities");
        foreach (var protoGroup in prototypeGroups.Cast<MappingDataNode>())
        {
            EntProtoId? protoId = null;
            var deletedPrototype = false;
            if (protoGroup.TryGet<ValueDataNode>("proto", out var protoIdNode)
                && !string.IsNullOrWhiteSpace(protoIdNode.Value))
            {
                if (DeletedPrototypes.Contains(protoIdNode.Value))
                {
                    deletedPrototype = true;
                    if (_proto.HasIndex<EntityPrototype>(protoIdNode.Value))
                        protoId = protoIdNode.Value;
                }
                else if (RenamedPrototypes.TryGetValue(protoIdNode.Value, out var newType))
                    protoId = newType;
                else
                    protoId = protoIdNode.Value;
            }

            var entities = (SequenceDataNode) protoGroup["entities"];
            _proto.TryIndex(protoId, out var proto);

            var protoData = Prototypes.GetOrNew(proto?.ID ?? string.Empty);
            foreach (var entityNode in entities.Cast<MappingDataNode>())
            {
                var yamlId = entityNode.Get<ValueDataNode>("uid").AsInt();
                var entData = new EntData(yamlId, entityNode, PostInit: !preInit, Paused: preInit, ToDelete: deletedPrototype);
                protoData.Add(entData);
                YamlEntities.Add(yamlId, entData);
            }
        }
    }


    private void ReadTileMap()
    {
        // Load tile mapping so that we can map the stored tile IDs into the ones actually used at runtime.
        _stopwatch.Restart();
        var tileMap = Data.Get<MappingDataNode>("tilemap");
        var migrations = new Dictionary<string, string>();
        foreach (var proto in _proto.EnumeratePrototypes<TileAliasPrototype>())
        {
            migrations.Add(proto.ID, proto.Target);
        }

        foreach (var (key, value) in tileMap.Children)
        {
            var yamlTileId = ((ValueDataNode) key).AsInt();
            var tileName = ((ValueDataNode) value).Value;
            if (migrations.TryGetValue(tileName, out var @new))
                tileName = @new;

            TileMap.Add(yamlTileId, tileName);
        }

        _log.Debug($"Read tilemap in {_stopwatch.Elapsed}");
    }

    private void AllocateEntities()
    {
        _stopwatch.Restart();

        foreach (var (protoId, ents) in Prototypes)
        {
            var proto = protoId == string.Empty
                ? null
                : _proto.Index<EntityPrototype>(protoId);

            foreach (var ent in ents)
            {
                var entity = EntMan.AllocEntity(proto);
                Result.Entities.Add(entity);
                UidMap.Add(ent.YamlId, entity);
                Entities.Add(entity, ent);

                if (ent.PostInit)
                    PostMapInit.Add(entity);

                if (ent.Paused)
                    Paused.Add(entity);

                if (ent.ToDelete)
                    ToDelete.Add(entity);

                if (Options.StoreYamlUids)
                    EntMan.AddComponent<YamlUidComponent>(entity).Uid = ent.YamlId;
            }
        }

        _log.Debug($"Allocated {Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private void ReadMapsAndGrids()
    {
        if (Result.Version < 7)
            return;

        var maps = Data.Get<SequenceDataNode>("maps");
        foreach (var node in maps)
        {
            var yamlId = ((ValueDataNode) node).AsInt();
            MapYamlIds.Add(yamlId);
        }

        var grids = Data.Get<SequenceDataNode>("grids");
        foreach (var node in grids)
        {
            var yamlId = ((ValueDataNode) node).AsInt();
            GridYamlIds.Add(yamlId);
        }

        var orphans = Data.Get<SequenceDataNode>("orphans");
        foreach (var node in orphans)
        {
            var yamlId = ((ValueDataNode) node).AsInt();
            OrphanYamlIds.Add(yamlId);
        }
    }

    private void LoadEntities()
    {
        _stopwatch.Restart();
        foreach (var (entity, data) in Entities)
        {
            try
            {
                CurrentReadingEntity = data;
                LoadEntity(entity, _metaQuery.Comp(entity), data.Node);
            }
            catch (Exception e)
            {
#if !EXCEPTION_TOLERANCE
                throw;
#endif
                ToDelete.Add(entity);
                _log.Error($"Encountered error while loading entity. Yaml uid: {data.YamlId}. Loaded loaded entity: {EntMan.ToPrettyString(entity)}. Error:\n{e}.");
            }
        }

        CurrentReadingEntity = null;
        _log.Debug($"Loaded {Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private void LoadEntity(EntityUid uid, MetaDataComponent meta, MappingDataNode entData)
    {
        CurrentReadingEntityComponents.Clear();
        CurrentlyIgnoredComponents.Clear();

        if (entData.TryGet("components", out SequenceDataNode? componentList))
        {
            var prototype = meta.EntityPrototype;
            CurrentReadingEntityComponents.EnsureCapacity(componentList.Count);
            foreach (var compData in componentList.Cast<MappingDataNode>())
            {
                var value = ((ValueDataNode)compData["type"]).Value;
                if (!_factory.TryGetRegistration(value, out var reg))
                {
                    if (!_factory.IsIgnored(value))
                        _log.Error($"Encountered unregistered component ({value}) while loading entity {EntMan.ToPrettyString(uid)}");
                    continue;
                }

                var compType = reg.Type;
                MappingDataNode datanode;
                if (prototype?.Components != null && prototype.Components.TryGetValue(value, out var protoData))
                {
                    // Previously this method used generic composition pushing. I.e.:
                    /*
                     datanode = ISerializationManager.PushCompositionWithGenericNode(
                        compData,
                        [protoData.Mapping],
                        datanode,
                        this);
                    */
                    // However, I don't think this is what we want to do here. I.e., we want to ignore things like the
                    // AlwaysPushInheritanceAttribute. Complex inheritance pushing should have already been done when
                    // creating the proto data. Now we just want to override the prototype information with the
                    // serialized data.
                    //
                    // If we do ever want to support this, we need to change entity serialization so that it doesn't do
                    // a simple diff with respect to the prototype data and instead does some kind of inheritance
                    // subtraction / removal.

                    datanode = _seriMan.CombineMappings(compData, protoData.Mapping);
                }
                else
                {
                    datanode = compData.ShallowClone();
                }


                datanode.Remove("type");
                CurrentComponent = value;
                CurrentReadingEntityComponents[value] = (IComponent) _seriMan.Read(compType, datanode, this)!;
                CurrentComponent = null;
            }
        }

        if (entData.TryGet("missingComponents", out SequenceDataNode? missingComponentList))
            CurrentlyIgnoredComponents = missingComponentList.Cast<ValueDataNode>().Select(x => x.Value).ToHashSet();

        EntityPrototype.LoadEntity((uid, meta), _factory, EntMan, _seriMan, this);

        if (CurrentlyIgnoredComponents.Count > 0)
            meta.LastComponentRemoved = Timing.CurTick;
    }

    private void GetMapsAndGrids()
    {
        if (Result.Version < 7)
        {
            GetMapsAndGridsFallback();
            return;
        }

        foreach (var yamlId in MapYamlIds)
        {
            var uid = UidMap[yamlId];
            if (_mapQuery.TryComp(uid, out var map))
            {
                Result.Maps.Add((uid, map));
                EntMan.EnsureComponent<LoadedMapComponent>(uid);
            }
            else
                _log.Error($"Missing map entity: {EntMan.ToPrettyString(uid)}");
        }

        foreach (var yamlId in GridYamlIds)
        {
            var uid = UidMap[yamlId];
            if (_gridQuery.TryComp(uid, out var grid))
                Result.Grids.Add((uid, grid));
            else
                _log.Error($"Missing grid entity: {EntMan.ToPrettyString(uid)}");
        }

        foreach (var yamlId in OrphanYamlIds)
        {
            var uid = UidMap[yamlId];
            if (EntMan.HasComponent<MapComponent>(uid) || _xformQuery.Comp(uid).ParentUid.IsValid())
                _log.Error($"Entity {EntMan.ToPrettyString(uid)} was incorrectly labelled as an orphan?");
            else
                Result.Orphans.Add(uid);
        }
    }

    public void AssignMapIds()
    {
        foreach (var map in Result.Maps)
        {
            _map.AssignMapId(map);
        }
    }

    private void GetMapsAndGridsFallback()
    {
        foreach (var uid in Result.Entities)
        {
            if (_gridQuery.TryComp(uid, out var grid))
            {
                Result.Grids.Add((uid, grid));
                if (_xformQuery.Comp(uid).ParentUid == EntityUid.Invalid && !_mapQuery.HasComp(uid))
                    Result.Orphans.Add(uid);
            }

            if (_mapQuery.TryComp(uid, out var map))
            {
                Result.Maps.Add((uid, map));
                EntMan.EnsureComponent<LoadedMapComponent>(uid);
            }
        }
    }

    private void RemoveEmptyChunks()
    {
        var gridQuery = EntMan.GetEntityQuery<MapGridComponent>();
        foreach (var uid in Entities.Keys)
        {
            if (!gridQuery.TryGetComponent(uid, out var gridComp))
                continue;

            foreach (var (index, chunk) in gridComp.Chunks)
            {
                if (chunk.FilledTiles > 0)
                    continue;

                _log.Warning(
                    $"Encountered empty chunk while deserializing map. Grid: {EntMan.ToPrettyString(uid)}. Chunk index: {index}");
                gridComp.Chunks.Remove(index);
            }
        }
    }

    private void StoreGridTileMap()
    {
        /*
        if (TileMap.Count == 0)
            return;
            */

        foreach (var entity in Result.Grids)
        {
            EntMan.EnsureComponent<MapSaveTileMapComponent>(entity).TileMap = TileMap.ShallowClone();
        }
    }

    private void BuildEntityHierarchy()
    {
        _stopwatch.Restart();
        var processed = new HashSet<EntityUid>(Result.Entities.Count);

        foreach (var ent in Result.Entities)
        {
            BuildEntityHierarchy(ent, processed);
        }

        _log.Debug($"Built entity hierarchy for {Result.Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    /// <summary>
    /// Validate that the category read from the metadata section is correct
    /// </summary>
    private void CheckCategory()
    {
        if (Result.Version < 7)
        {
            InferCategory();
            return;
        }

        switch (Result.Category)
        {
            case FileCategory.Map:
                if (Result.Maps.Count == 1 && Result.Orphans.Count == 0)
                    return;
                _log.Error($"Expected file to contain a single map, but instead found {Result.Maps.Count} maps and {Result.Orphans.Count} orphans");
                break;

            case FileCategory.Grid:
                if (Result.Maps.Count == 0 && Result.Grids.Count == 1 && Result.Orphans.Count == 1 && Result.Orphans.First() == Result.Grids.First().Owner)
                    return;
                _log.Error($"Expected file to contain a single grid, but instead found {Result.Grids.Count} grids and {Result.Orphans.Count} orphans");
                break;

            case FileCategory.Entity:
                if (Result.Maps.Count == 0 && Result.Grids.Count == 0 && Result.Orphans.Count == 1)
                    return;
                _log.Error($"Expected file to contain a orphaned entity, but instead found {Result.Orphans.Count} orphans");
                break;

            default:
                return; // No validation for full game saves
        }
    }

    private void InferCategory()
    {
        if (Result.Category != FileCategory.Unknown)
            return;

        if (Result.Maps.Count == 1)
            Result.Category = FileCategory.Map;
        else if (Result.Maps.Count == 0 && Result.Grids.Count == 1)
            Result.Category = FileCategory.Grid;
    }

    /// <summary>
    /// In case there are any "orphaned" grids, we want to ensure that they all have a map before we initialize them,
    /// as grids in null-space are not currently supported.
    /// </summary>
    private void AdoptGrids()
    {
        foreach (var grid in Result.Grids)
        {
            if (EntMan.HasComponent<MapComponent>(grid.Owner))
                continue;

            var xform = _xformQuery.Comp(grid.Owner);
            if (xform.ParentUid.IsValid())
                continue;

            DebugTools.Assert(Result.Orphans.Contains(grid.Owner));
            if (Options.LogOrphanedGrids)
                _log.Error($"Encountered an orphaned grid. Automatically creating a map for the grid.");
            var map = _map.CreateUninitializedMap();
            _map.AssignMapId(map);

            // We intentionally do this after maps have been given the LoadedMapComponent, so this map will not have it.
            // vague justification is that this entity wasn't actually deserialized from the file, and shouldn't
            // contain any non-default data.

            // But the real reason is that this is just how it used to work due to shitty code that never properly
            // distinguished between grid & map files, and checks for this component after deserialization to check whether
            // the file was a grid or map.

            // So we still support code that tries to load a file without knowing what's in it, but unless the options
            // disable it, the default behaviour is to log an error in this situation. This is meant to try ensure that
            // people use the `TryLoadGrid` method when appropriate.

            Result.Entities.Add(map);
            Result.Maps.Add(map);
            Result.Orphans.Remove(grid.Owner);
            xform._parent = map.Owner;
            DebugTools.Assert(!xform._mapIdInitialized);
        }
    }

    /// <summary>
    /// Verify that all map entities have been assigned a map id.
    /// </summary>
    private void ValidateMapIds()
    {
        foreach (var map in Result.Maps)
        {
            if (map.Comp.MapId == MapId.Nullspace
                || !_map.TryGetMap(map.Comp.MapId, out var e)
                || e != map.Owner)
            {
                throw new Exception($"Map entity {EntMan.ToPrettyString(map)} has not been assigned a map id");
            }
        }
    }

    private void PauseMaps()
    {
        if (!Options.PauseMaps)
            return;

        foreach (var ent in Result.Maps)
        {
            _map.SetPaused(ent!, true);
        }
    }

    private void BuildEntityHierarchy(EntityUid uid, HashSet<EntityUid> processed)
    {
        // If we've already added it then skip.
        if (!processed.Add(uid))
            return;

        if (!_xformQuery.TryComp(uid, out var xform))
            return;

        // Ensure parent is done first.
        var parent = xform.ParentUid;
        if (parent != EntityUid.Invalid)
            BuildEntityHierarchy(parent, processed);

        // If entities were moved around or merged onto an existing map, it is possible that the entities passed
        // to this method were not originally being deserialized.
        if (!Result.Entities.Contains(uid))
            return;

        SortedEntities.Add(uid);
        if (parent == EntityUid.Invalid)
            Result.RootNodes.Add(uid);
    }

    private void ProcessNullspaceEntities()
    {
        foreach (var uid in Result.RootNodes)
        {
            if (EntMan.HasComponent<MapComponent>(uid))
            {
                DebugTools.Assert(Result.Maps.Any(x => x.Owner == uid));
                continue;
            }

            if (Result.Orphans.Contains(uid))
                continue;

            Result.NullspaceEntities.Add(uid);

            // Null-space grids are not yet supported.
            // So it shouldn't have been possible to save a grid without it being flagged as an orphan.
            DebugTools.Assert(!EntMan.HasComponent<MapGridComponent>(uid));
        }
    }

    private void StartEntitiesInternal()
    {
        _stopwatch.Restart();
        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        foreach (var uid in SortedEntities)
        {
            StartupEntity(uid, metaQuery.GetComponent(uid));
        }
        _log.Debug($"Started up {Result.Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private void StartupEntity(EntityUid uid, MetaDataComponent metadata)
    {
        ResetNetTicks(uid, metadata);
        EntMan.InitializeEntity(uid, metadata);
        EntMan.StartEntity(uid);
    }

    private void ResetNetTicks(EntityUid uid, MetaDataComponent metadata)
    {
        if (!Entities.TryGetValue(uid, out var entData))
        {
            // AdoptGrids() can create new maps that have no associated yaml data.
            return;
        }

        if (metadata.EntityPrototype is not { } prototype)
            return;

        if (!entData.Node.TryGet("components", out SequenceDataNode? componentList))
            return;

        foreach (var component in metadata.NetComponents.Values)
        {
            var compName = _factory.GetComponentName(component.GetType());

            if (componentList.Cast<MappingDataNode>().Any(p => ((ValueDataNode) p["type"]).Value == compName))
            {
                if (prototype.Components.ContainsKey(compName))
                {
                    // This component is modified by the map so we have to send state.
                    // Though it's still in the prototype itself so creation doesn't need to be sent.
                    component.ClearCreationTick();
                }

                continue;
            }

            // This component is not modified by the map file,
            // so the client will have the same data after instantiating it from prototype ID.
            component.ClearTicks();
        }
    }

    private void SetMapInitLifestage()
    {
        if (PostMapInit.Count == 0)
            return;

        _stopwatch.Restart();

        foreach (var uid in PostMapInit)
        {
            if (!_metaQuery.TryComp(uid, out var meta))
                continue;

            DebugTools.Assert(meta.EntityLifeStage == EntityLifeStage.Initialized);
            meta.EntityLifeStage = EntityLifeStage.MapInitialized;
        }

        _log.Debug($"Finished flagging mapinit in {_stopwatch.Elapsed}");
    }

    private void SetPaused()
    {
        if (Paused.Count == 0)
            return;

        _stopwatch.Restart();

        var time = Timing.CurTime;
        var ev = new EntityPausedEvent();

        foreach (var uid in Paused)
        {
            if (!_metaQuery.TryComp(uid, out var meta))
                continue;

            meta.PauseTime = time;

            // TODO ENTITY SERIALIZATION
            // TODO PowerNet / NodeNet Serialization
            // Make this **not** raise an event.
            // Ideally an event shouldn't be required. However some stinky systems rely on it to actually pause entities.
            // E.g., power nets don't get serialized properly, and store entity-paused information in a separate object,
            // so they needs to receive the event to make sure that the entity is **actually** paused.
            // hours of debugging
            // AAAAAAAAaaaa
            // Whenever node nets become nullspace ents, this can probably just be purged.
            EntMan.EventBus.RaiseLocalEvent(uid, ref ev);
        }

        _log.Debug($"Finished setting PauseTime in {_stopwatch.Elapsed}");
    }


    private void InitializeMaps()
    {
        if (!Options.InitializeMaps)
        {
            if (Options.PauseMaps)
                return; // Already paused

            foreach (var ent in Result.Maps)
            {
                if (_metaQuery.Comp(ent.Owner).EntityLifeStage < EntityLifeStage.MapInitialized)
                    _map.SetPaused(ent!, true);
            }

            return;
        }

        foreach (var ent in Result.Maps)
        {
            if (!ent.Comp.MapInitialized)
                _map.InitializeMap(ent!, unpause: !Options.PauseMaps);
        }
    }

    private void ProcessDeletions()
    {
        foreach (var uid in ToDelete)
        {
            EntMan.DeleteEntity(uid);
            Result.Entities.Remove(uid);
        }
    }

    // Create custom object serializers that will correctly allow data to be overriden by the map file.
    bool IEntityLoadContext.TryGetComponent(string componentName, [NotNullWhen(true)] out IComponent? component)
    {
        return CurrentReadingEntityComponents.TryGetValue(componentName, out component);
    }

    public IEnumerable<string> GetExtraComponentTypes()
    {
        return CurrentReadingEntityComponents.Keys;
    }

    public bool ShouldSkipComponent(string compName)
    {
        return CurrentlyIgnoredComponents.Contains(compName);
    }

    #region ITypeSerializer

    ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        if (node.Value is "null" or "invalid")
            return new ValidatedValueNode(node);

        if (!int.TryParse(node.Value, out _))
            return new ErrorNode(node, "Invalid EntityUid");

        return new ValidatedValueNode(node);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        EntityUid value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return value.IsValid()
            ? new ValueDataNode(value.Id.ToString(CultureInfo.InvariantCulture))
            : new ValueDataNode("invalid");
    }

    EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<EntityUid>? _)
    {
        if (node.Value == "invalid")
        {
            if (CurrentComponent == "Transform")
                return EntityUid.Invalid;

            if (!Options.LogInvalidEntities)
                return EntityUid.Invalid;

            var msg = CurrentReadingEntity is not { } curr
                ? $"Encountered invalid EntityUid reference"
                : $"Encountered invalid EntityUid reference wile reading entity {curr.YamlId}, component: {CurrentComponent}";
            _log.Error(msg);
            return EntityUid.Invalid;
        }

        if (int.TryParse(node.Value, out var val) && UidMap.TryGetValue(val, out var entity))
            return entity;

        _log.Error($"Invalid yaml entity id: '{val}'");
        return EntityUid.Invalid;
    }

    [MustUseReturnValue]
    public EntityUid Copy(
        ISerializationManager serializationManager,
        EntityUid source,
        EntityUid target,
        bool skipHook,
        ISerializationContext? context = null)
    {
        return new(source.Id);
    }

    #endregion
}
