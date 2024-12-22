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
internal sealed class EntityDeserializer : ISerializationContext, IEntityLoadContext,
    ITypeSerializer<EntityUid, ValueDataNode>
{
    private const int BackwardsVersion = 3;

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
    /// Subset of the file's <see cref="Data"/> relevant to each entity.
    /// </summary>
    public readonly Dictionary<EntityUid, EntData> Entities = new();

    /// <summary>
    /// Entity data grouped by their entity prototype id. Any entities without a prototype or with an invalid or
    /// deleted prototypes use an empty string.
    /// </summary>
    public readonly Dictionary<string, List<EntData>> Prototypes = new();

    public readonly record struct EntData(int YamlId, MappingDataNode Node, bool PostInit, bool Paused, bool ToDelete);

    public readonly LoadResult Result = new();
    public readonly Dictionary<int, string> TileMap = new();
    public readonly Dictionary<int, EntityUid> UidMap = new();
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
    /// This processes some of the data in <see cref="Data"/>, including extracting the metdata, tile-map,
    /// validate that all referenced entity prototypes exists, grouping entity data by their prototypes.
    /// </summary>
    /// <returns>Returns false if the entity data cannot be processed</returns>
    public bool ProcessData()
    {
        ReadMetadata();
        if (Result.Version < BackwardsVersion)
        {
            _log.Error(
                $"Cannot handle this map file version, found v{Result.Version} and require at least v{BackwardsVersion}");
            return false;
        }

        if (!ValidatePrototypes())
            return false;

        ReadEntities();
        ReadTileMap();
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

        // Read the list of maps, grids, and orphan entities
        ReadMapsAndGrids();

        // grids prior to engine v175 might've been serialized with empty chunks which now throw debug asserts.
        RemoveEmptyChunks();

        // Assign MapSaveTileMapComponent to all read grids. This is used to avoid large file diffs if the tile map changes.
        StoreGridTileMap();

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
        PauseMaps();
        BuildEntityHierarchy();
        ProcessNullspaceEntities();
        StartEntitiesInternal();
        SetMapInitLifestage();
        SetPaused();
        MapInitializeEntities();
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

                protoData.Add(new EntData(yamlId, entityNode, postInit, paused, deletedPrototype));
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
        }
    }

    private void ReadEntitiesFallback()
    {
        var metadata = Data.Get<MappingDataNode>("meta");
        var preInit = metadata.TryGet<ValueDataNode>("postmapinit", out var mapInitNode) && !mapInitNode.AsBool();

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
                protoData.Add(new EntData(yamlId, entityNode, PostInit: !preInit, Paused: preInit, ToDelete: deletedPrototype));
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
                var datanode = compData.Copy();
                datanode.Remove("type");
                var value = ((ValueDataNode)compData["type"]).Value;
                if (!_factory.TryGetRegistration(value, out var reg))
                {
                    if (!_factory.IsIgnored(value))
                        _log.Error($"Encountered unregistered component ({value}) while loading entity {EntMan.ToPrettyString(uid)}");
                    continue;
                }

                var compType = reg.Type;
                if (prototype?.Components != null && prototype.Components.TryGetValue(value, out var protoData))
                {
                    datanode = _seriMan.PushCompositionWithGenericNode(
                            compType,
                            [protoData.Mapping],
                            datanode,
                            this);
                }

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

    private void ReadMapsAndGrids()
    {
        if (Result.Version < 7)
        {
            ReadMapsAndGridsFallback();
            return;
        }

        var maps = Data.Get<SequenceDataNode>("maps");
        foreach (var node in maps)
        {
            var yamlId = ((ValueDataNode) node).AsInt();
            var uid = UidMap[yamlId];
            if (_mapQuery.TryComp(uid, out var map))
            {
                Result.Maps.Add((uid, map));
                EntMan.EnsureComponent<LoadedMapComponent>(uid);
            }
            else
                _log.Error($"Missing map entity: {EntMan.ToPrettyString(uid)}");
        }

        var grids = Data.Get<SequenceDataNode>("grids");
        foreach (var node in grids)
        {
            var yamlId = ((ValueDataNode) node).AsInt();
            var uid = UidMap[yamlId];
            if (_gridQuery.TryComp(uid, out var grid))
                Result.Grids.Add((uid, grid));
            else
                _log.Error($"Missing grid entity: {EntMan.ToPrettyString(uid)}");
        }

        var orphans = Data.Get<SequenceDataNode>("orphans");
        foreach (var node in orphans)
        {
            var yamlId = ((ValueDataNode) node).AsInt();
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

    private void ReadMapsAndGridsFallback()
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
                if (Result.Maps.Count == 1)
                    return;
                _log.Error($"Expected file to contain a single map, but instead found: {Result.Maps.Count}");
                break;

            case FileCategory.Grid:
                if (Result.Maps.Count == 0 && Result.Grids.Count == 1)
                    return;
                _log.Error($"Expected file to contain a single grid, but instead found: {Result.Grids.Count}");
                break;

            case FileCategory.Entity:
                if (Result.Maps.Count == 0 && Result.Grids.Count == 0 && Result.Orphans.Count == 1)
                    return;
                _log.Error($"Expected file to contain a orphaned entity, but instead found: {Result.Orphans.Count}");
                break;

            default:
                return;
        }

        Result.Category = FileCategory.Unknown;
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


    private void MapInitializeEntities()
    {
        if (!Options.InitializeMaps)
        {
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
        if (node.Value == "invalid" && CurrentComponent == "Transform")
            return EntityUid.Invalid;

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
