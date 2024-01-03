using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.GameObjects;

public sealed class MapLoaderSystem : EntitySystem
{
    /*
     * Not a partial of MapSystem so we don't have to deal with additional test dependencies.
     */

    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
                 private          IServerEntityManagerInternal _serverEntityManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _logLoader = default!;
    private ISawmill _logWriter = default!;

    private static readonly MapLoadOptions DefaultLoadOptions = new();
    private const int MapFormatVersion = 6;
    private const int BackwardsVersion = 2;

    private MapSerializationContext _context = default!;
    private Stopwatch _stopwatch = new();

    public override void Initialize()
    {
        base.Initialize();
        _serverEntityManager = (IServerEntityManagerInternal)EntityManager;
        _logLoader = Logger.GetSawmill("loader");
        _logWriter = Logger.GetSawmill("writer");
        _logLoader.Level = LogLevel.Info;
        _context = new MapSerializationContext(_serverEntityManager, _timing);
    }

    #region Public

    [Obsolete("Use TryLoad")]
    public EntityUid? LoadGrid(MapId mapId, string path, MapLoadOptions? options = null)
    {
        if (!TryLoad(mapId, path, out var grids, options))
        {
            DebugTools.Assert(false);
            return null;
        }

        var actualGrids = new List<EntityUid>();
        var gridQuery = GetEntityQuery<MapGridComponent>();

        foreach (var ent in grids)
        {
            if (!gridQuery.HasComponent(ent))
                continue;

            actualGrids.Add(ent);
        }

        DebugTools.Assert(actualGrids.Count == 1);
        return actualGrids[0];
    }

    [Obsolete("Use TryLoad")]
    public IReadOnlyList<EntityUid> LoadMap(MapId mapId, string path, MapLoadOptions? options = null)
    {
        if (TryLoad(mapId, path, out var grids, options))
        {
            var actualGrids = new List<EntityUid>();
            var gridQuery = GetEntityQuery<MapGridComponent>();

            foreach (var ent in grids)
            {
                if (!gridQuery.HasComponent(ent))
                    continue;

                actualGrids.Add(ent);
            }

            return actualGrids;
        }

        DebugTools.Assert(false);
        return new List<EntityUid>();
    }

    public void Load(MapId mapId, string path, MapLoadOptions? options = null)
    {
        TryLoad(mapId, path, out _, options);
    }

    /// <summary>
    /// Tries to load the supplied path onto the supplied Mapid.
    /// Will return false if something went wrong and needs handling.
    /// </summary>
    /// <param name="mapId">The Mapid to load onto. Depending on the supplied options this map may or may not already exist.</param>
    /// <param name="path">The resource path to the required map.</param>
    /// <param name="rootUids">The root Uids of the map; not guaranteed to be grids!</param>
    /// <param name="options">The required options for loading.</param>
    /// <returns></returns>
    public bool TryLoad(MapId mapId, string path, [NotNullWhen(true)] out IReadOnlyList<EntityUid>? rootUids,
        MapLoadOptions? options = null)
    {
        options ??= DefaultLoadOptions;

        var resPath = new ResPath(path).ToRootedPath();

        if (!TryGetReader(resPath, out var reader))
        {
            rootUids = new List<EntityUid>();
            return false;
        }

        bool result;

        using (reader)
        {
            _logLoader.Info($"Loading Map: {resPath}");

            _stopwatch.Restart();
            var data = new MapData(mapId, reader, options);
            _logLoader.Debug($"Loaded yml stream in {_stopwatch.Elapsed}");
            var sw = new Stopwatch();
            sw.Start();
            result = Deserialize(data);
            _logLoader.Debug($"Loaded map in {sw.Elapsed}");

            var mapEnt = _mapManager.GetMapEntityId(mapId);
            var xformQuery = _serverEntityManager.GetEntityQuery<TransformComponent>();
            var rootEnts = new List<EntityUid>();
            // aeoeoeieioe content

            if (HasComp<MapGridComponent>(mapEnt))
            {
                rootEnts.Add(mapEnt);
            }
            else
            {
                foreach (var ent in data.Entities)
                {
                    if (xformQuery.GetComponent(ent).ParentUid == mapEnt)
                        rootEnts.Add(ent);
                }
            }

            rootUids = rootEnts;
        }

        _context.Clear();

#if DEBUG
        DebugTools.Assert(result);
#endif

        return result;
    }

    public void Save(EntityUid uid, string ymlPath)
    {
        if (!Exists(uid))
        {
            _logLoader.Error($"Unable to find entity {uid} for saving.");
            return;
        }

        if (Transform(uid).MapUid == null)
        {
            _logLoader.Error($"Found invalid map for {ToPrettyString(uid)}, aborting saving.");
            return;
        }

        _logLoader.Debug($"Saving entity {ToPrettyString(uid)} to {ymlPath}");

        var document = new YamlDocument(GetSaveData(uid).ToYaml());

        var resPath = new ResPath(ymlPath).ToRootedPath();
        _resourceManager.UserData.CreateDir(resPath.Directory);

        using var writer = _resourceManager.UserData.OpenWriteText(resPath);
        {
            var stream = new YamlStream { document };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        _logLoader.Info($"Saved {ToPrettyString(uid)} to {ymlPath}");
    }

    public void SaveMap(MapId mapId, string ymlPath)
    {
        if (!_mapManager.MapExists(mapId))
        {
            _logLoader.Error($"Unable to find map {mapId}");
            return;
        }

        Save(_mapManager.GetMapEntityId(mapId), ymlPath);
    }

    #endregion

    #region Loading

    private bool TryGetReader(ResPath resPath, [NotNullWhen(true)] out TextReader? reader)
    {
        // try user
        if (!_resourceManager.UserData.Exists(resPath))
        {
            _logLoader.Info($"No user map found: {resPath}");

            // fallback to content
            if (_resourceManager.TryContentFileRead(resPath, out var contentReader))
            {
                reader = new StreamReader(contentReader);
            }
            else
            {
                _logLoader.Error($"No map found: {resPath}");
                reader = null;
                return false;
            }
        }
        else
        {
            reader = _resourceManager.UserData.OpenText(resPath);
        }

        return true;
    }

    private bool Deserialize(MapData data)
    {
        var ev = new BeforeEntityReadEvent();
        RaiseLocalEvent(ev);

        // First we load map meta data like version.
        if (!ReadMetaSection(data))
            return false;

        // Verify that prototypes for all the entities exist
        if (!VerifyEntitiesExist(data, ev))
            return false;

        // Tile map
        ReadTileMapSection(data);

        // Alloc entities
        var toDelete = AllocEntities(data, ev);

        // Load the prototype data onto entities, e.g. transform parents, etc.
        LoadEntities(data);

        // Build the scene graph / transform hierarchy to know the order to startup entities.
        // This also allows us to swap out the root node up front if necessary.
        BuildEntityHierarchy(data);

        // From hierarchy work out root node; if we're loading onto an existing map then see if we need to swap out
        // the root from the yml.
        SwapRootNode(data);

        ReadGrids(data);

        // grids prior to engine v175 might've been serialized with empty chunks which now throw debug asserts.
        RemoveEmptyChunks(data);

        // Then, go hierarchically in order and do the entity things.
        StartupEntities(data);

        // At the very end, delete entities belonging to removed prototypes. This is being done after startup just in
        // case these entities have any children that somehow rely on startup in order to properly shut down.
        // This is pretty cursed and might cause unexpected errors.
        foreach (var uid in toDelete)
        {
           Del(uid);
           data.Entities.Remove(uid);
        }

        return true;
    }

    private void RemoveEmptyChunks(MapData data)
    {
        var gridQuery = _serverEntityManager.GetEntityQuery<MapGridComponent>();
        foreach (var uid in data.EntitiesToDeserialize.Keys)
        {
            if (!gridQuery.TryGetComponent(uid, out var gridComp))
                continue;

            foreach (var (index, chunk) in gridComp.Chunks)
            {
                if (chunk.FilledTiles > 0)
                    continue;

                Log.Warning($"Encountered empty chunk while deserializing map. Grid: {ToPrettyString(uid)}. Chunk index: {index}");
                gridComp.Chunks.Remove(index);
            }
        }
    }

    private bool VerifyEntitiesExist(MapData data, BeforeEntityReadEvent ev)
    {
        _stopwatch.Restart();
        var fail = false;
        var reportedError = new HashSet<string>();
        var key = data.Version >= 4 ? "proto" : "type";
        var entities = data.RootMappingNode.Get<SequenceDataNode>("entities");

        foreach (var metaDef in entities.Cast<MappingDataNode>())
        {
            if (!metaDef.TryGet<ValueDataNode>(key, out var typeNode))
                continue;

            var type = typeNode.Value;
            if (string.IsNullOrWhiteSpace(type))
                continue;

            if (ev.RenamedPrototypes.TryGetValue(type, out var newType))
                type = newType;

            if (_prototypeManager.HasIndex<EntityPrototype>(type))
                continue;

            if (!reportedError.Add(type))
                continue;

            if (ev.DeletedPrototypes.Contains(type))
            {
                _logLoader.Warning("Map contains an obsolete/removed prototype: {0}. This may cause unexpected errors.", type);
                continue;
            }

            _logLoader.Error("Missing prototype for map: {0}", type);
            fail = true;
            reportedError.Add(type);
        }

        _logLoader.Debug($"Verified entities in {_stopwatch.Elapsed}");

        if (fail)
        {
            _logLoader.Error("Found missing prototypes in map file. Missing prototypes have been dumped to logs.");
            return false;
        }

        return true;
    }

    private bool ReadMetaSection(MapData data)
    {
        var meta = data.RootMappingNode.Get<MappingDataNode>("meta");
        var ver = meta.Get<ValueDataNode>("format").AsInt();
        if (ver < BackwardsVersion)
        {
            _logLoader.Error($"Cannot handle this map file version, found {ver} and require {MapFormatVersion}");
            return false;
        }

        data.Version = ver;

        if (meta.TryGet<ValueDataNode>("postmapinit", out var mapInitNode))
        {
            data.MapIsPostInit = mapInitNode.AsBool();
        }
        else
        {
            data.MapIsPostInit = true;
        }

        return true;
    }

    private void ReadTileMapSection(MapData data)
    {
        _stopwatch.Restart();

        // Load tile mapping so that we can map the stored tile IDs into the ones actually used at runtime.
        var tileMap = data.RootMappingNode.Get<MappingDataNode>("tilemap");
        _context.TileMap = new Dictionary<int, string>(tileMap.Count);

        foreach (var (key, value) in tileMap.Children)
        {
            var tileId = ((ValueDataNode)key).AsInt();
            var tileDefName = ((ValueDataNode)value).Value;
            _context.TileMap.Add(tileId, tileDefName);
        }

        _logLoader.Debug($"Read tilemap in {_stopwatch.Elapsed}");
    }

    private HashSet<EntityUid> AllocEntities(MapData data, BeforeEntityReadEvent ev)
    {
        _stopwatch.Restart();
        var mapUid = _mapManager.GetMapEntityId(data.TargetMap);
        var pauseTime = mapUid.IsValid() ? _meta.GetPauseTime(mapUid) : TimeSpan.Zero;
        _context.Set(data.UidEntityMap, new Dictionary<EntityUid, int>(), data.MapIsPostInit, pauseTime, null);
        HashSet<EntityUid> deletedPrototypeUids = new();

        if (data.Version >= 4)
        {
            var metaEntities = data.RootMappingNode.Get<SequenceDataNode>("entities");

            foreach (var metaDef in metaEntities.Cast<MappingDataNode>())
            {
                string? type = null;
                var deletedPrototype = false;
                if (metaDef.TryGet<ValueDataNode>("proto", out var typeNode)
                    && !string.IsNullOrWhiteSpace(typeNode.Value))
                {

                    if (ev.DeletedPrototypes.Contains(typeNode.Value))
                        deletedPrototype = true;
                    else if (ev.RenamedPrototypes.TryGetValue(typeNode.Value, out var newType))
                        type = newType;
                    else
                        type = typeNode.Value;
                }

                var entities = (SequenceDataNode) metaDef["entities"];
                EntityPrototype? proto = null;

                if (type != null)
                    _prototypeManager.TryIndex(type, out proto);

                foreach (var entityDef in entities.Cast<MappingDataNode>())
                {
                    var uid = entityDef.Get<ValueDataNode>("uid").AsInt();

                    var entity = _serverEntityManager.AllocEntity(proto);
                    data.Entities.Add(entity);
                    data.UidEntityMap.Add(uid, entity);
                    data.EntitiesToDeserialize.Add(entity, entityDef);

                    if (deletedPrototype)
                    {
                        deletedPrototypeUids.Add(entity);
                    }
                    else if (data.Options.StoreMapUids)
                    {
                        var comp = _serverEntityManager.AddComponent<MapSaveIdComponent>(entity);
                        comp.Uid = uid;
                    }
                }
            }
        }
        else
        {
            var entities = data.RootMappingNode.Get<SequenceDataNode>("entities");

            foreach (var entityDef in entities.Cast<MappingDataNode>())
            {
                EntityUid entity;
                if (entityDef.TryGet<ValueDataNode>("type", out var typeNode))
                {
                    if (ev.DeletedPrototypes.Contains(typeNode.Value))
                    {
                        entity = _serverEntityManager.AllocEntity(null);
                        deletedPrototypeUids.Add(entity);
                    }
                    else if (ev.RenamedPrototypes.TryGetValue(typeNode.Value, out var newType))
                    {
                        _prototypeManager.TryIndex<EntityPrototype>(newType, out var prototype);
                        entity = _serverEntityManager.AllocEntity(prototype);
                    }
                    else
                    {
                        _prototypeManager.TryIndex<EntityPrototype>(typeNode.Value, out var prototype);
                        entity = _serverEntityManager.AllocEntity(prototype);
                    }
                }
                else
                {
                    entity = _serverEntityManager.AllocEntity(null);
                }

                var uid = entityDef.Get<ValueDataNode>("uid").AsInt();
                data.Entities.Add(entity);
                data.UidEntityMap.Add(uid, entity);
                data.EntitiesToDeserialize.Add(entity, entityDef);

                if (data.Options.StoreMapUids)
                {
                    var comp = _serverEntityManager.AddComponent<MapSaveIdComponent>(entity);
                    comp.Uid = uid;
                }
            }
        }

        _logLoader.Debug($"Allocated {data.Entities.Count} entities in {_stopwatch.Elapsed}");
        return deletedPrototypeUids;
    }

    private void LoadEntities(MapData mapData)
    {
        _stopwatch.Restart();
        var metaQuery = GetEntityQuery<MetaDataComponent>();

        foreach (var (entity, data) in mapData.EntitiesToDeserialize)
        {
            LoadEntity(entity, data, metaQuery.GetComponent(entity));
        }

        _logLoader.Debug($"Loaded {mapData.Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private void LoadEntity(EntityUid uid, MappingDataNode data, MetaDataComponent meta)
    {
        _context.CurrentReadingEntityComponents.Clear();
        _context.CurrentlyIgnoredComponents.Clear();

        if (data.TryGet("components", out SequenceDataNode? componentList))
        {
            var prototype = meta.EntityPrototype;
            _context.CurrentReadingEntityComponents.EnsureCapacity(componentList.Count);
            foreach (var compData in componentList.Cast<MappingDataNode>())
            {
                var datanode = compData.Copy();
                datanode.Remove("type");
                var value = ((ValueDataNode)compData["type"]).Value;
                if (!_factory.TryGetRegistration(value, out var reg))
                {
                    if (!_factory.IsIgnored(value))
                        _logLoader.Error($"Encountered unregistered component ({value}) while loading entity {ToPrettyString(uid)}");
                    continue;
                }

                var compType = reg.Type;
                if (prototype?.Components != null && prototype.Components.TryGetValue(value, out var protData))
                {
                    datanode =
                        _serManager.PushCompositionWithGenericNode(
                            compType,
                            new[] { protData.Mapping }, datanode, _context);
                }

                _context.CurrentComponent = value;
                _context.CurrentReadingEntityComponents[value] = (IComponent) _serManager.Read(compType, datanode, _context)!;
                _context.CurrentComponent = null;
            }
        }

        if (data.TryGet("missingComponents", out SequenceDataNode? missingComponentList))
            _context.CurrentlyIgnoredComponents = missingComponentList.Cast<ValueDataNode>().Select(x => x.Value).ToHashSet();

        _serverEntityManager.FinishEntityLoad(uid, meta.EntityPrototype, _context);
        if (_context.CurrentlyIgnoredComponents.Count > 0)
            meta.LastComponentRemoved = _timing.CurTick;
    }

    private void BuildEntityHierarchy(MapData mapData)
    {
        _stopwatch.Restart();
        var hierarchy = mapData.Hierarchy;
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var ent in mapData.Entities)
        {
            if (xformQuery.TryGetComponent(ent, out var xform))
            {
                hierarchy[ent] = xform.ParentUid;
            }
            else
            {
                hierarchy[ent] = EntityUid.Invalid;
            }
        }

        // mapData.Entities = new List<EntityUid>(mapData.Entities.Count);
        var added = new HashSet<EntityUid>(mapData.Entities.Count);
        mapData.Entities.Clear();

        while (hierarchy.Count > 0)
        {
            var enumerator = hierarchy.GetEnumerator();
            enumerator.MoveNext();
            var (current, parent) = enumerator.Current;
            BuildTopology(hierarchy, added, mapData.Entities, current, parent);
            enumerator.Dispose();
        }

        _logLoader.Debug($"Built entity hierarchy for {mapData.Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private void BuildTopology(Dictionary<EntityUid, EntityUid> hierarchy, HashSet<EntityUid> added, List<EntityUid> Entities, EntityUid current, EntityUid parent)
    {
        // If we've already added it then skip.
        if (!added.Add(current))
            return;

        // Ensure parent is done first.
        if (hierarchy.TryGetValue(parent, out var parentValue))
        {
            BuildTopology(hierarchy, added, Entities, parent, parentValue);
        }

        DebugTools.Assert(current.IsValid());
        // DebugTools.Assert(!Entities.Contains(current));
        Entities.Add(current);
        hierarchy.Remove(current);
    }

    private void SwapRootNode(MapData data)
    {
        _stopwatch.Restart();

        // There's 4 scenarios
        // 1. We're loading a map file onto an existing map. Dump the map file's map and use the existing one
        // 2. We're loading a map file onto an existing map. Use the map file's map and swap entities to it.
        // 3. We're loading a map file onto a new map. Use CreateMap (for now) and swap out the uid to the correct one
        // 4. We're loading a non-map file; in this case it depends whether the map exists or not, then proceed with the above.

        var rootNode = data.Entities[0];
        var xformQuery = GetEntityQuery<TransformComponent>();
        // We just need to cache the old mapuid and point to the new mapuid.

        if (HasComp<MapComponent>(rootNode))
        {
            // If map exists swap out
            if (_mapManager.MapExists(data.TargetMap))
            {
                // Map exists but we also have a map file with stuff on it soooo swap out the old map.
                if (data.Options.LoadMap)
                {
                    _logLoader.Info($"Loading map file with a root node onto an existing map!");

                    // Smelly
                    if (HasComp<MapGridComponent>(rootNode))
                    {
                        data.Options.Offset = Vector2.Zero;
                        data.Options.Rotation = Angle.Zero;
                    }

                    _mapManager.SetMapEntity(data.TargetMap, rootNode);
                    EnsureComp<LoadedMapComponent>(rootNode);
                }
                // Otherwise just ignore the map in the file.
                else
                {
                    var oldRootUid = data.Entities[0];
                    var newRootUid = _mapManager.GetMapEntityId(data.TargetMap);
                    data.Entities[0] = newRootUid;

                    foreach (var ent in data.Entities)
                    {
                        if (ent == newRootUid)
                            continue;

                        var xform = xformQuery.GetComponent(ent);

                        if (!xform.ParentUid.IsValid() || xform.ParentUid.Equals(oldRootUid))
                        {
                            _transform.SetParent(ent, xform, newRootUid);
                        }
                    }

                    Del(oldRootUid);
                }
            }
            else
            {
                // If we're loading a file with a map then swap out the entityuid
                // TODO: Mapmanager nonsense
                var AAAAA = _mapManager.CreateMap(data.TargetMap);

                if (!data.MapIsPostInit)
                {
                    _mapManager.AddUninitializedMap(data.TargetMap);
                }

                _mapManager.SetMapEntity(data.TargetMap, rootNode);
                EnsureComp<LoadedMapComponent>(rootNode);

                // Nothing should have invalid uid except for the root node.
            }
        }
        else
        {
            // No map file root, in that case create a new map / get the one we're loading onto.
            var mapNode = _mapManager.GetMapEntityId(data.TargetMap);

            if (!mapNode.IsValid())
            {
                // Map doesn't exist so we'll start it up now so we can re-attach the preinit entities to it for later.
                _mapManager.CreateMap(data.TargetMap);
                _mapManager.AddUninitializedMap(data.TargetMap);
                mapNode = _mapManager.GetMapEntityId(data.TargetMap);
                DebugTools.Assert(mapNode.IsValid());
            }

            // If anything has an invalid parent (e.g. it's some form of root node) then parent it to the map.
            foreach (var ent in data.Entities)
            {
                // If it's the map itself don't reparent.
                if (ent.Equals(mapNode))
                    continue;

                var xform = xformQuery.GetComponent(ent);

                if (!xform.ParentUid.IsValid())
                {
                    _transform.SetParent(ent, xform, mapNode);
                }
            }
        }

        data.MapIsPaused = _mapManager.IsMapPaused(data.TargetMap);
        _logLoader.Debug($"Swapped out root node in {_stopwatch.Elapsed}");
    }

    private void ReadGrids(MapData data)
    {
        // TODO: Kill this when we get map format v3 and remove grid-specific yml area.

        // MapGrids already contain their assigned GridId from their ctor, and the MapComponents just got deserialized.
        // Now we need to actually bind the MapGrids to their components so that you can resolve GridId -> EntityUid
        // After doing this, it should be 100% safe to use the MapManager API like normal.

        if (data.Version != BackwardsVersion)
            return;

        var yamlGrids = data.RootMappingNode.Get<SequenceDataNode>("grids");

        // There were no new grids, nothing to do here.
        if (yamlGrids.Count == 0)
            return;

        // get ents that the grids will bind to
        var gridComps = new Entity<MapGridComponent>[yamlGrids.Count];
        var gridQuery = _serverEntityManager.GetEntityQuery<MapGridComponent>();

        // linear search for new grid comps
        foreach (var uid in data.EntitiesToDeserialize.Keys)
        {
            if (!gridQuery.TryGetComponent(uid, out var gridComp))
                continue;

            // These should actually be new, pre-init
            DebugTools.Assert(gridComp.LifeStage == ComponentLifeStage.Added);

            gridComps[gridComp.GridIndex] = new Entity<MapGridComponent>(uid, gridComp);
        }

        for (var index = 0; index < yamlGrids.Count; index++)
        {
            // Here is where the implicit index pairing magic happens from the yaml.
            var yamlGrid = (MappingDataNode)yamlGrids[index];

            // designed to throw if something is broken, every grid must map to an ent
            var gridComp = gridComps[index];

            // TODO Once maps have been updated (save+load), remove GridComponent.GridIndex altogether and replace it with:
            // var savedUid = ((ValueDataNode)yamlGrid["uid"]).Value;
            // var gridUid = UidEntityMap[int.Parse(savedUid)];
            // var gridComp = gridQuery.GetComponent(gridUid);

            MappingDataNode yamlGridInfo = (MappingDataNode)yamlGrid["settings"];
            SequenceDataNode yamlGridChunks = (SequenceDataNode)yamlGrid["chunks"];

            AllocateMapGrid(gridComp, yamlGridInfo);
            var gridUid = gridComp.Owner;

            foreach (var chunkNode in yamlGridChunks.Cast<MappingDataNode>())
            {
                var (chunkOffsetX, chunkOffsetY) = _serManager.Read<Vector2i>(chunkNode["ind"]);
                _serManager.Read(chunkNode, _context, instanceProvider: () => _mapSystem.GetOrAddChunk(gridUid, gridComp, chunkOffsetX, chunkOffsetY), notNullableOverride: true);
            }
        }
    }

    private static void AllocateMapGrid(MapGridComponent gridComp, MappingDataNode yamlGridInfo)
    {
        // sane defaults
        ushort csz = 16;
        ushort tsz = 1;

        foreach (var kvInfo in yamlGridInfo)
        {
            var key = ((ValueDataNode)kvInfo.Key).Value;
            var val = ((ValueDataNode)kvInfo.Value).Value;
            if (key == "chunksize")
                csz = ushort.Parse(val);
            else if (key == "tilesize")
                tsz = ushort.Parse(val);
            else if (key == "snapsize")
                continue; // obsolete
        }

        gridComp.ChunkSize = csz;
        gridComp.TileSize = tsz;
    }

    private void StartupEntities(MapData data)
    {
        _stopwatch.Restart();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var rootEntity = data.Entities[0];
        var mapQuery = GetEntityQuery<MapComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        // If the root node is a map that's already existing don't bother with it.
        // If we're loading a grid then the map is already started up elsewhere in which case this
        // just loads the grid outside of the loop which is also fine.
        if (MetaData(rootEntity).EntityLifeStage < EntityLifeStage.Initialized)
        {
            StartupEntity(rootEntity, metaQuery.GetComponent(rootEntity), data);

            if (xformQuery.TryGetComponent(rootEntity, out var xform) && IsRoot(xform, mapQuery) && !HasComp<MapComponent>(rootEntity))
            {
                _transform.SetLocalPosition(xform, data.Options.TransformMatrix.Transform(xform.LocalPosition));
                xform.LocalRotation += data.Options.Rotation;
            }
        }

        for (var i = 1; i < data.Entities.Count; i++)
        {
            var entity = data.Entities[i];

            if (xformQuery.TryGetComponent(entity, out var xform) && IsRoot(xform, mapQuery))
            {
                // Don't want to trigger events
                xform._localPosition = data.Options.TransformMatrix.Transform(xform.LocalPosition);
                if (!xform.NoLocalRotation)
                    xform._localRotation += data.Options.Rotation;

                DebugTools.Assert(!xform.NoLocalRotation || xform.LocalRotation == 0);
            }

            StartupEntity(entity, metaQuery.GetComponent(entity), data);
        }

        _logLoader.Debug($"Started up {data.Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private bool IsRoot(TransformComponent xform, EntityQuery<MapComponent> mapQuery)
    {
        return !xform.ParentUid.IsValid() || mapQuery.HasComponent(xform.ParentUid);
    }

    private void StartupEntity(EntityUid uid, MetaDataComponent metadata, MapData data)
    {
        ResetNetTicks(uid, metadata, data.EntitiesToDeserialize[uid]);

        var isPaused = data is { MapIsPaused: true, MapIsPostInit: false };
        _meta.SetEntityPaused(uid, isPaused, metadata);

        // TODO: Apply map transforms if root node.
        _serverEntityManager.FinishEntityInitialization(uid, metadata);
        _serverEntityManager.FinishEntityStartup(uid);

        if (data.MapIsPostInit)
        {
            EntityManager.SetLifeStage(metadata, EntityLifeStage.MapInitialized);
        }
        else if (_mapManager.IsMapInitialized(data.TargetMap))
        {
            _serverEntityManager.RunMapInit(uid, metadata);
        }
    }

    private void ResetNetTicks(EntityUid entity, MetaDataComponent metadata, MappingDataNode data)
    {
        if (!data.TryGet("components", out SequenceDataNode? componentList))
        {
            return;
        }

        if (metadata.EntityPrototype is not {} prototype)
        {
            return;
        }

        foreach (var component in metadata.NetComponents.Values)
        {
            var compName = _factory.GetComponentName(component.GetType());

            if (componentList.Cast<MappingDataNode>().Any(p => ((ValueDataNode)p["type"]).Value == compName))
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

    #endregion

    #region Saving

    public MappingDataNode GetSaveData(EntityUid uid)
    {
        var ev = new BeforeSaveEvent(uid, Transform(uid).MapUid);
        RaiseLocalEvent(ev);

        var data = new MappingDataNode();
        WriteMetaSection(data, uid);

        var entityUidMap = new Dictionary<EntityUid, int>();
        var uidEntityMap = new Dictionary<int, EntityUid>();
        var entities = new List<EntityUid>();

        _stopwatch.Restart();
        PopulateEntityList(uid, entities, uidEntityMap, entityUidMap);
        WriteTileMapSection(data, entities);

        _logLoader.Debug($"Populated entity list in {_stopwatch.Elapsed}");
        var metadata = Comp<MetaDataComponent>(uid);
        var pauseTime = _meta.GetPauseTime(uid, metadata);

        // TODO replace MapPreInit with the map's entity lifestage
        // Yes, post-init maps do not have EntityLifeStage >= EntityLifeStage.MapInitialized
        bool postInit;
        if (TryComp(uid, out MapComponent? mapComp))
            postInit = !mapComp.MapPreInit;
        else
            postInit = metadata.EntityLifeStage >= EntityLifeStage.MapInitialized;

        var rootXform = _serverEntityManager.GetComponent<TransformComponent>(uid);
        _context.Set(uidEntityMap, entityUidMap, postInit, pauseTime, rootXform.ParentUid);

        _stopwatch.Restart();
        WriteEntitySection(data, uidEntityMap, entityUidMap);
        _logLoader.Debug($"Wrote entity section for {entities.Count} entities in {_stopwatch.Elapsed}");
        _context.Clear();

        return data;
    }

    private void WriteMetaSection(MappingDataNode rootNode, EntityUid uid)
    {
        var meta = new MappingDataNode();
        rootNode.Add("meta", meta);
        meta.Add("format", MapFormatVersion.ToString(CultureInfo.InvariantCulture));

        var xform = Transform(uid);
        var isPostInit = _mapManager.IsMapInitialized(xform.MapID);

        meta.Add("postmapinit", isPostInit ? "true" : "false");
    }

    private void WriteTileMapSection(MappingDataNode rootNode, List<EntityUid> entities)
    {
        // Although we could use tiledefmanager it might write tiledata we don't need so we'll compress it
        var gridQuery = GetEntityQuery<MapGridComponent>();
        var tileDefs = new HashSet<int>();

        foreach (var ent in entities)
        {
            if (!gridQuery.TryGetComponent(ent, out var grid))
                continue;

            var tileEnumerator = grid.GetAllTilesEnumerator(false);

            while (tileEnumerator.MoveNext(out var tileRef))
            {
                tileDefs.Add(tileRef.Value.Tile.TypeId);
            }
        }

        var tileMap = new MappingDataNode();
        rootNode.Add("tilemap", tileMap);
        var ordered = new List<int>(tileDefs);
        ordered.Sort();

        foreach (var tyleId in ordered)
        {
            var tileDef = _tileDefManager[tyleId];
            tileMap.Add(tyleId.ToString(CultureInfo.InvariantCulture), tileDef.ID);
        }
    }

    private void PopulateEntityList(EntityUid uid, List<EntityUid> entities, Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        var withoutUid = new HashSet<EntityUid>();
        var saveCompQuery = GetEntityQuery<MapSaveIdComponent>();
        var transformCompQuery = GetEntityQuery<TransformComponent>();
        var metaCompQuery = GetEntityQuery<MetaDataComponent>();

        RecursivePopulate(uid, entities, uidEntityMap, withoutUid, metaCompQuery, transformCompQuery, saveCompQuery);

        var uidCounter = 1;
        foreach (var entity in withoutUid)
        {
            while (uidEntityMap.ContainsKey(uidCounter))
            {
                // Find next available UID.
                uidCounter += 1;
            }

            uidEntityMap.Add(uidCounter, entity);
            uidCounter += 1;
        }

        // Build a reverse lookup
        entityUidMap.EnsureCapacity(uidEntityMap.Count);
        foreach(var (saveId, mapId) in uidEntityMap)
        {
            entityUidMap.Add(mapId, saveId);
        }
    }

    private bool IsSaveable(EntityUid uid, EntityQuery<MetaDataComponent> metaQuery, EntityQuery<TransformComponent> transformQuery)
    {
        // Don't serialize things parented to un savable things.
        // For example clothes inside a person.
        while (uid.IsValid())
        {
            var meta = metaQuery.GetComponent(uid);

            if (meta.EntityDeleted || meta.EntityPrototype?.MapSavable == false) break;

            uid = transformQuery.GetComponent(uid).ParentUid;
        }

        // If we manage to get up to the map (root node) then it's saveable.
        return !uid.IsValid();
    }

    private void RecursivePopulate(EntityUid uid,
        List<EntityUid> entities,
        Dictionary<int, EntityUid> uidEntityMap,
        HashSet<EntityUid> withoutUid,
        EntityQuery<MetaDataComponent> metaQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<MapSaveIdComponent> saveCompQuery)
    {
        if (!IsSaveable(uid, metaQuery, transformQuery))
            return;

        entities.Add(uid);

        // TODO: Given there's some structure to this now we can probably omit the parent / child a bit.
        if (!saveCompQuery.TryGetComponent(uid, out var mapSaveComp)
            || mapSaveComp.Uid == 0
            || !uidEntityMap.TryAdd(mapSaveComp.Uid, uid))
        {
            // If the id was already saved before, or has no save component we need to find a new id for this entity
            withoutUid.Add(uid);
        }

        var xform = transformQuery.GetComponent(uid);
        foreach (var child in xform._children)
        {
            RecursivePopulate(child, entities, uidEntityMap, withoutUid, metaQuery, transformQuery, saveCompQuery);
        }
    }

    private void WriteEntitySection(MappingDataNode rootNode, Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var metaName = _factory.GetComponentName(typeof(MetaDataComponent));
        var xformName = _factory.GetComponentName(typeof(TransformComponent));

        // As metadata isn't on components we'll special-case it.
        var prototypeCompCache = new Dictionary<string, Dictionary<string, MappingDataNode>>();

        var emptyMetaNode = _serManager.WriteValueAs<MappingDataNode>(typeof(MetaDataComponent), new MetaDataComponent(), alwaysWrite: true, context: _context);

        _context.CurrentComponent = _factory.GetComponentName(typeof(TransformComponent));
        var emptyXformNode = _serManager.WriteValueAs<MappingDataNode>(typeof(TransformComponent), new TransformComponent(), alwaysWrite: true, context: _context);
        _context.CurrentComponent = null;

        var prototypes = new Dictionary<string, List<int>>();

        foreach (var (entityUid, saveId) in entityUidMap)
        {
            var meta = metaQuery.GetComponent(entityUid);

            if (!_context.MapInitialized && meta.EntityLifeStage >= EntityLifeStage.MapInitialized)
                _logWriter.Error($"Encountered a post-init entity in a pre-init map. Entity: {ToPrettyString(entityUid)}");

            var id = meta.EntityPrototype?.ID;
            id ??= string.Empty;
            var uids = prototypes.GetOrNew(id);
            uids.Add(saveId);
        }

        var protos = prototypes.Keys.ToList();
        protos.Sort();
        var entityPrototypes = new SequenceDataNode();
        rootNode.Add("entities", entityPrototypes);

        foreach (var proto in protos)
        {
            var saveIds = prototypes[proto];
            saveIds.Sort();
            var entities = new SequenceDataNode();

            var node = new MappingDataNode()
            {
                { "proto", proto },
                { "entities", entities},
            };

            entityPrototypes.Add(node);

            foreach (var saveId in saveIds)
            {
                var entityUid = uidEntityMap[saveId];

                _context.CurrentWritingEntity = entityUid;
                var mapping = new MappingDataNode
                {
                    {"uid", saveId.ToString(CultureInfo.InvariantCulture)}
                };

                var md = metaQuery.GetComponent(entityUid);

                Dictionary<string, MappingDataNode>? cache = null;

                if (md.EntityPrototype is {} prototype)
                {
                    if (!prototypeCompCache.TryGetValue(prototype.ID, out cache))
                    {
                        prototypeCompCache[prototype.ID] = cache = new Dictionary<string, MappingDataNode>(prototype.Components.Count);
                        _context.WritingReadingPrototypes = true;

                        foreach (var (compType, comp) in prototype.Components)
                        {
                            _context.CurrentComponent = compType;
                            cache.Add(compType, _serManager.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component, alwaysWrite: true, context: _context));
                        }

                        _context.CurrentComponent = null;
                        _context.WritingReadingPrototypes = false;
                        cache.TryAdd(metaName, emptyMetaNode);
                        cache.TryAdd(xformName, emptyXformNode);
                    }
                }

                var components = new SequenceDataNode();

                var xform = Transform(entityUid);
                if (xform.NoLocalRotation && xform.LocalRotation != 0)
                {
                    Log.Error($"Encountered a no-rotation entity with non-zero local rotation: {ToPrettyString(entityUid)}");
                    xform._localRotation = 0;
                }

                foreach (var component in EntityManager.GetComponents(entityUid))
                {
                    if (component is MapSaveIdComponent)
                        continue;

                    var compType = component.GetType();
                    var compName = _factory.GetComponentName(compType);
                    _context.CurrentComponent = compName;
                    MappingDataNode? compMapping;
                    MappingDataNode? protMapping = null;
                    if (cache != null && cache.TryGetValue(compName, out protMapping))
                    {
                        // If this has a prototype, we need to use alwaysWrite: true.
                        // E.g., an anchored prototype might have anchored: true. If we we are saving an un-anchored
                        // instance of this entity, and if we have alwaysWrite: false, then compMapping would not include
                        // the anchored data-field (as false is the default for this bool data field), so the entity would
                        // implicitly be saved as anchored.
                        compMapping = _serManager.WriteValueAs<MappingDataNode>(compType, component, alwaysWrite: true,
                            context: _context);

                        // This will NOT recursively call Except() on the values of the mapping. It will only remove
                        // key-value pairs if both the keys and values are equal.
                        compMapping = compMapping.Except(protMapping);
                        if(compMapping == null)
                            continue;
                    }
                    else
                    {
                        compMapping = _serManager.WriteValueAs<MappingDataNode>(compType, component, alwaysWrite: false,
                            context: _context);
                    }

                    // Don't need to write it if nothing was written! Note that if this entity has no associated
                    // prototype, we ALWAYS want to write the component, because merely the fact that it exists is
                    // information that needs to be written.
                    if (compMapping.Children.Count != 0 || protMapping == null)
                    {
                        compMapping.InsertAt(0, "type", new ValueDataNode(compName));
                        // Something actually got written!
                        components.Add(compMapping);
                    }
                }

                if (components.Count != 0)
                {
                    mapping.Add("components", components);
                }

                if (md.EntityPrototype == null)
                {
                    // No prototype - we are done.
                    entities.Add(mapping);
                    continue;
                }

                // an entity may have less components than the original prototype, so we need to check if any are missing.
                var missingComponents = new SequenceDataNode();
                foreach (var (name, comp) in md.EntityPrototype.Components)
                {
                    // try comp instead of has-comp as it checks whether the component is supposed to have been
                    // deleted.
                    if (_serverEntityManager.TryGetComponent(entityUid, comp.Component.GetType(), out _))
                        continue;

                    missingComponents.Add(new ValueDataNode(name));
                }

                if (missingComponents.Count != 0)
                {
                    mapping.Add("missingComponents", missingComponents);
                }

                entities.Add(mapping);
            }
        }
    }

    #endregion

    /// <summary>
    ///     Does basic pre-deserialization checks on map file load.
    ///     For example, let's not try to use maps with multiple grids as blueprints, shall we?
    /// </summary>
    private sealed class MapData
    {
        public MappingDataNode RootMappingNode { get; }

        public readonly MapId TargetMap;
        public bool MapIsPostInit;
        public bool MapIsPaused;
        public readonly MapLoadOptions Options;
        public int Version;

        // Loading data
        public readonly List<EntityUid> Entities = new();
        public readonly Dictionary<int, EntityUid> UidEntityMap = new();
        public readonly Dictionary<EntityUid, MappingDataNode> EntitiesToDeserialize = new();

        public readonly Dictionary<EntityUid, EntityUid> Hierarchy = new();

        public MapData(MapId mapId, TextReader reader, MapLoadOptions options)
        {
            var documents = DataNodeParser.ParseYamlStream(reader).ToArray();

            if (documents.Length < 1)
            {
                throw new InvalidDataException("Stream has no YAML documents.");
            }

            // Kinda wanted to just make this print a warning and pick [0] but screw that.
            // What is this, a hug box?
            if (documents.Length > 1)
            {
                throw new InvalidDataException("Stream too many YAML documents. Map files store exactly one.");
            }

            RootMappingNode = (MappingDataNode) documents[0].Root!;
            Options = options;
            TargetMap = mapId;
        }
    }
}
