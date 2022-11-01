using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Prometheus;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Robust.Server.Maps;

public sealed partial class MapManagerSystem
{
    private ISawmill _logLoader = default!;

    private static readonly MapLoadOptions DefaultLoadOptions = new();
    private const int MapFormatVersion = 2;

    private MapSerializationContext _context = default!;
    private Stopwatch _stopwatch = new();

    public override void Initialize()
    {
        base.Initialize();
        _serverEntityManager = (IServerEntityManagerInternal)EntityManager;
        _logLoader = Logger.GetSawmill("loader");
        _context = new MapSerializationContext(_factory, EntityManager, _serManager);
    }

    #region Public

    public bool TryLoad(MapId mapId, string path, [NotNullWhen(true)] out IReadOnlyList<EntityUid>? gridUids,
        MapLoadOptions? options = null)
    {
        options ??= DefaultLoadOptions;
        gridUids = new List<EntityUid>();

        if (options.LoadMap && _mapManager.MapExists(mapId))
        {
            _logLoader.Error($"Tried to load map file {path} onto existing map {mapId} without overwriting the existing map?");
#if DEBUG
            DebugTools.Assert(false);
#endif
            return false;
        }

        var resPath = new ResourcePath(path).ToRootedPath();

        if (!TryGetReader(resPath, out var reader))
        {
            return false;
        }

        bool result;

        using (reader)
        {
            _logLoader.Info($"Loading Map: {resPath}");

            _stopwatch.Restart();
            var data = new MapData(mapId, reader, options);
            _logLoader.Debug($"Loaded yml stream in {_stopwatch.Elapsed}");
            result = Deserialize(data);
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

        _logLoader.Info($"Saving entity {ToPrettyString(uid)} to {ymlPath}");

        var document = new YamlDocument(GetSaveData(uid).ToYaml());

        var resPath = new ResourcePath(ymlPath).ToRootedPath();
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

    private bool TryGetReader(ResourcePath resPath, [NotNullWhen(true)] out TextReader? reader)
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
        // Verify that prototypes for all the entities exist
        if (!VerifyEntitiesExist(data))
            return false;

        // First we load map meta data like version.
        if (!ReadMetaSection(data))
            return false;

        // Tile map
        ReadTileMapSection(data);

        // Alloc entities
        AllocEntities(data);

        // Load the prototype data onto entities, e.g. transform parents, etc.
        LoadEntities(data);

        // Build the scene graph / transform hierarchy to know the order to startup entities.
        // This also allows us to swap out the root node up front if necessary.
        BuildEntityHierarchy(data);

        // From hierarchy work out root node; if we're loading onto an existing map then see if we need to swap out
        // the root from the yml.
        SwapRootNode(data);

        ReadGrids(data);

        // Then, go hierarchically in order and do the entity things.
        StartupEntities(data);

        return true;
    }

    private bool VerifyEntitiesExist(MapData data)
    {
        _stopwatch.Restart();
        var fail = false;
        var entities = data.RootMappingNode.Get<SequenceDataNode>("entities");
        var reportedError = new HashSet<string>();

        foreach (var entityDef in entities.Cast<MappingDataNode>())
        {
            if (entityDef.TryGet<ValueDataNode>("type", out var typeNode))
            {
                var type = typeNode.Value;
                if (!_prototypeManager.HasIndex<EntityPrototype>(type) && !reportedError.Contains(type))
                {
                    Logger.ErrorS("map", "Missing prototype for map: {0}", type);
                    fail = true;
                    reportedError.Add(type);
                }
            }
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
        if (ver != MapFormatVersion)
        {
            _logLoader.Error($"Cannot handle this map file version, found {ver} and require {MapFormatVersion}");
            return false;
        }

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
        _context.TileMap = new Dictionary<ushort, string>(tileMap.Count);

        foreach (var (key, value) in tileMap.Children)
        {
            var tileId = (ushort) ((ValueDataNode)key).AsInt();
            var tileDefName = ((ValueDataNode)value).Value;
            _context.TileMap.Add(tileId, tileDefName);
        }

        _logLoader.Debug($"Read tilemap in {_stopwatch.Elapsed}");
    }

    private void AllocEntities(MapData data)
    {
        _stopwatch.Restart();
        var entities = data.RootMappingNode.Get<SequenceDataNode>("entities");
        _context.Set(data.UidEntityMap, new Dictionary<EntityUid, int>());
        data.Entities.EnsureCapacity(entities.Count);
        data.UidEntityMap.EnsureCapacity(entities.Count);
        data.EntitiesToDeserialize.EnsureCapacity(entities.Count);

        foreach (var entityDef in entities.Cast<MappingDataNode>())
        {
            string? type = null;
            if (entityDef.TryGet<ValueDataNode>("type", out var typeNode))
            {
                type = typeNode.Value;
            }

            // TODO Fix this. If the entities are ever defined out of order, and if one of them does not have a
            // "uid" node, then defaulting to Entities.Count will error.
            var uid = data.Entities.Count;

            if (entityDef.TryGet<ValueDataNode>("uid", out var uidNode))
            {
                uid = uidNode.AsInt();
            }

            var entity = _serverEntityManager.AllocEntity(type);
            data.Entities.Add(entity);
            data.UidEntityMap.Add(uid, entity);
            data.EntitiesToDeserialize.Add(entity, entityDef);

            if (data.Options.StoreMapUids)
            {
                var comp = _serverEntityManager.AddComponent<MapSaveIdComponent>(entity);
                comp.Uid = uid;
            }
        }

        _logLoader.Debug($"Allocated {data.Entities.Count} entities in {_stopwatch.Elapsed}");
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

        if (data.TryGet("components", out SequenceDataNode? componentList))
        {
            foreach (var compData in componentList.Cast<MappingDataNode>())
            {
                var datanode = compData.Copy();
                datanode.Remove("type");
                var value = ((ValueDataNode)compData["type"]).Value;
                _context.CurrentReadingEntityComponents[value] = datanode;
            }
        }

        _serverEntityManager.FinishEntityLoad(uid, meta.EntityPrototype, _context);
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

        mapData.InitOrder = new List<EntityUid>(mapData.Entities.Count);
        var added = new HashSet<EntityUid>(mapData.Entities.Count);

        while (hierarchy.Count > 0)
        {
            var enumerator = hierarchy.GetEnumerator();
            enumerator.MoveNext();
            var (current, parent) = enumerator.Current;
            BuildTopology(hierarchy, added, mapData.InitOrder, current, parent);
            enumerator.Dispose();
        }

        _logLoader.Debug($"Built entity hierarchy for {mapData.Entities.Count} entities in {_stopwatch.Elapsed}");
    }

    private void BuildTopology(Dictionary<EntityUid, EntityUid> hierarchy, HashSet<EntityUid> added, List<EntityUid> initOrder, EntityUid current, EntityUid parent)
    {
        // If we've already added it then skip.
        if (!added.Add(current))
            return;

        // Ensure parent is done first.
        if (hierarchy.TryGetValue(parent, out var parentValue))
        {
            BuildTopology(hierarchy, added, initOrder, parent, parentValue);
        }

        DebugTools.Assert(current.IsValid());
        DebugTools.Assert(!initOrder.Contains(current));
        initOrder.Add(current);
        hierarchy.Remove(current);
    }

    private void SwapRootNode(MapData data)
    {
        _stopwatch.Restart();

        // There's 3 scenarios
        // 1. We're loading a map file onto an existing map. In this case dump the map file map and use the existing map
        // 2. We're loading a map file onto a new map. Use CreateMap (for now) and swap out the uid to the correct one
        // 3. We're loading a non-map file; in this case it depends whether the map exists or not, then proceed with the above.

        var rootNode = data.InitOrder[0];
        var xformQuery = GetEntityQuery<TransformComponent>();
        // We just need to cache the old mapuid and point to the new mapuid.

        if (HasComp<MapComponent>(rootNode))
        {
            // If map exists swap out
            if (_mapManager.MapExists(data.TargetMap))
            {
                if (data.Options.LoadMap)
                {
                    _logLoader.Warning($"Loading map file with a root node onto an existing map!");
                }

                var oldRootUid = data.InitOrder[0];
                var newRootUid = _mapManager.GetMapEntityId(data.TargetMap);
                data.InitOrder[0] = newRootUid;

                foreach (var ent in data.InitOrder)
                {
                    var xform = xformQuery.GetComponent(ent);

                    if (!xform.ParentUid.IsValid() || xform.ParentUid.Equals(oldRootUid))
                    {
                        xform.AttachParent(newRootUid);
                    }
                }

                data.MapIsPostInit = _mapManager.IsMapInitialized(data.TargetMap);
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
                mapNode = _mapManager.GetMapEntityId(data.TargetMap);
                DebugTools.Assert(mapNode.IsValid());
            }

            // If anything has an invalid parent (e.g. it's some form of root node) then parent it to the map.
            foreach (var ent in data.InitOrder)
            {
                // If it's the map itself don't reparent.
                if (ent.Equals(mapNode))
                    continue;

                var xform = xformQuery.GetComponent(ent);

                if (!xform.ParentUid.IsValid() || xform.ParentUid.Equals(rootNode))
                {
                    xform.AttachParent(mapNode);
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

        var yamlGrids = data.RootMappingNode.Get<SequenceDataNode>("grids");

        // There were no new grids, nothing to do here.
        if (yamlGrids.Count == 0)
            return;

        // get ents that the grids will bind to
        var gridComps = new MapGridComponent[yamlGrids.Count];

        var gridQuery = _serverEntityManager.GetEntityQuery<MapGridComponent>();

        // linear search for new grid comps
        foreach (var uid in data.EntitiesToDeserialize.Keys)
        {
            if (!gridQuery.TryGetComponent(uid, out var gridComp))
                continue;

            // These should actually be new, pre-init
            DebugTools.Assert(gridComp.LifeStage == ComponentLifeStage.Added);

            gridComps[gridComp.GridIndex] = gridComp;
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

            var grid = AllocateMapGrid(gridComp, yamlGridInfo);

            foreach (var chunkNode in yamlGridChunks.Cast<MappingDataNode>())
            {
                var (chunkOffsetX, chunkOffsetY) = _serManager.Read<Vector2i>(chunkNode["ind"]);
                var chunk = grid.GetChunk(chunkOffsetX, chunkOffsetY);
                _serManager.Read(chunkNode, _context, value: chunk);
            }
        }
    }

    private static MapGrid AllocateMapGrid(MapGridComponent gridComp, MappingDataNode yamlGridInfo)
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

        var grid = gridComp.AllocMapGrid(csz, tsz);

        return grid;
    }

    private void StartupEntities(MapData data)
    {
        _stopwatch.Restart();
        DebugTools.Assert(data.Entities.Count == data.InitOrder.Count);
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var rootEntity = data.InitOrder[0];

        // If the root node is a map that's already existing don't bother with it.
        // If we're loading a grid then the map is already started up elsewhere in which case this
        // just loads the grid outside of the loop which is also fine.
        if (MetaData(rootEntity).EntityLifeStage < EntityLifeStage.Initialized)
        {
            StartupEntity(rootEntity, metaQuery.GetComponent(rootEntity), data);
        }

        // TODO: For anything that's a root entity apply the transform adjustments.
        for (var i = 1; i < data.InitOrder.Count; i++)
        {
            var entity = data.InitOrder[i];
            StartupEntity(entity, metaQuery.GetComponent(entity), data);
        }

        _logLoader.Debug($"Started up {data.InitOrder.Count} entities in {_stopwatch.Elapsed}");
    }

    private void StartupEntity(EntityUid uid, MetaDataComponent metadata, MapData data)
    {
        ResetNetTicks(uid, metadata, data.EntitiesToDeserialize[uid]);
        // TODO: Apply map transforms if root node.
        _serverEntityManager.FinishEntityInitialization(uid, metadata);
        _serverEntityManager.FinishEntityStartup(uid);
        MapInit(uid, metadata, data);
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

        foreach (var (netId, component) in EntityManager.GetNetComponents(entity))
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

    private void MapInit(EntityUid uid, MetaDataComponent metadata, MapData data)
    {
        var isPaused = data.MapIsPaused;

        if (data.MapIsPostInit)
        {
            metadata.EntityLifeStage = EntityLifeStage.MapInitialized;
        }
        else if (_mapManager.IsMapInitialized(data.TargetMap))
        {
            EntityManager.RunMapInit(uid, metadata);

            if (isPaused)
                _meta.SetEntityPaused(uid, true, metadata);

        }
        else if (isPaused)
        {
            _meta.SetEntityPaused(uid, true, metadata);
        }
    }

    #endregion

    #region Saving

    private MappingDataNode GetSaveData(EntityUid uid)
    {
        var data = new MappingDataNode();
        WriteMetaSection(data, uid);
        WriteTileMapSection(data);

        var entityUidMap = new Dictionary<EntityUid, int>();
        var uidEntityMap = new Dictionary<int, EntityUid>();
        var entities = new List<EntityUid>();

        _stopwatch.Restart();
        PopulateEntityList(uid, entities, uidEntityMap, entityUidMap);
        _logLoader.Debug($"Populated entity list in {_stopwatch.Elapsed}");
        WriteGridSection(data, entities);

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
        // TODO: Make these values configurable.
        meta.Add("name", "DemoStation");
        meta.Add("author", "Space-Wizards");

        var xform = Transform(uid);
        var isPostInit = _mapManager.IsMapInitialized(xform.MapID);

        meta.Add("postmapinit", isPostInit ? "true" : "false");
    }

    private void WriteTileMapSection(MappingDataNode rootNode)
    {
        var tileMap = new MappingDataNode();
        rootNode.Add("tilemap", tileMap);
        foreach (var tileDefinition in _tileDefManager)
        {
            tileMap.Add(tileDefinition.TileId.ToString(CultureInfo.InvariantCulture), tileDefinition.ID);
        }
    }

    private void PopulateEntityList(EntityUid uid, List<EntityUid> entities, Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        var withoutUid = new HashSet<EntityUid>();
        var saveCompQuery = GetEntityQuery<MapSaveIdComponent>();
        var transformCompQuery = GetEntityQuery<TransformComponent>();
        var metaCompQuery = GetEntityQuery<MetaDataComponent>();

        RecursivePopulate(uid, entities, uidEntityMap, withoutUid, metaCompQuery, transformCompQuery, saveCompQuery);

        var uidCounter = 0;
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
        if (!saveCompQuery.TryGetComponent(uid, out var mapSaveComp) ||
            !uidEntityMap.TryAdd(mapSaveComp.Uid, uid))
        {
            // If the id was already saved before, or has no save component we need to find a new id for this entity
            withoutUid.Add(uid);
        }

        var enumerator = transformQuery.GetComponent(uid).ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            RecursivePopulate(child.Value, entities, uidEntityMap, withoutUid, metaQuery, transformQuery, saveCompQuery);
        }
    }

    private void WriteGridSection(MappingDataNode rootNode, List<EntityUid> entities)
    {
        // TODO: Shitcode
        var grids = new SequenceDataNode();
        rootNode.Add("grids", grids);
        var mapGridQuery = GetEntityQuery<MapGridComponent>();

        int index = 0;
        foreach (var entity in entities)
        {
            if (!mapGridQuery.TryGetComponent(entity, out var grid))
                continue;

            grid.GridIndex = index;
            var entry = _serManager.WriteValue(grid, context: _context);
            grids.Add(entry);
            index++;
        }
    }

    private void WriteEntitySection(MappingDataNode rootNode, Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var entities = new SequenceDataNode();
        rootNode.Add("entities", entities);

        var prototypeCompCache = new Dictionary<string, Dictionary<string, MappingDataNode>>();
        _context.Set(uidEntityMap, entityUidMap);

        foreach (var (saveId, entityUid) in uidEntityMap.OrderBy( e=> e.Key))
        {
            _context.CurrentWritingEntity = entityUid;
            var mapping = new MappingDataNode
            {
                {"uid", saveId.ToString(CultureInfo.InvariantCulture)}
            };

            var md = metaQuery.GetComponent(entityUid);

            Dictionary<string, MappingDataNode>? cache = null;

            if (md.EntityPrototype is {} prototype)
            {
                mapping.Add("type", prototype.ID);

                if (!prototypeCompCache.TryGetValue(prototype.ID, out cache))
                {
                    prototypeCompCache[prototype.ID] = cache = new Dictionary<string, MappingDataNode>(prototype.Components.Count);

                    foreach (var (compType, comp) in prototype.Components)
                    {
                        cache.Add(compType, _serManager.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component));
                    }
                }
            }

            var components = new SequenceDataNode();

            // See engine#636 for why the Distinct() call.
            foreach (var component in EntityManager.GetComponents(entityUid))
            {
                if (component is MapSaveIdComponent)
                    continue;

                var compType = component.GetType();
                var compName = _factory.GetComponentName(compType);
                _context.CurrentWritingComponent = compName;
                var compMapping = _serManager.WriteValueAs<MappingDataNode>(compType, component, context: _context);

                if (cache?.TryGetValue(compName, out var protMapping) == true)
                {
                    // This will NOT recursively call Except() on the values of the mapping. It will only remove
                    // key-value pairs if both the keys and values are equal.
                    compMapping = compMapping.Except(protMapping);
                    if(compMapping == null)
                        continue;
                }

                // Don't need to write it if nothing was written! Note that if this entity has no associated
                // prototype, we ALWAYS want to write the component, because merely the fact that it exists is
                // information that needs to be written.
                if (compMapping.Children.Count != 0 || md.EntityPrototype == null)
                {
                    compMapping.Add("type", new ValueDataNode(compName));
                    // Something actually got written!
                    components.Add(compMapping);
                }
            }

            if (components.Count != 0)
            {
                mapping.Add("components", components);
            }

            entities.Add(mapping);
        }
    }

    #endregion

    private sealed record MapLoaderData
    {
        public readonly MapId TargetMap;
        public readonly MappingDataNode RootNode;

        public MapLoaderData(MappingDataNode rootNode)
        {
            RootNode = rootNode;
        }
    }

    private sealed class MapDeserializationContext
    {

    }

    internal sealed class MapSerializationContext : ISerializationContext, IEntityLoadContext,
        ITypeSerializer<EntityUid, ValueDataNode>
    {
        private readonly IComponentFactory _factory;
        private readonly IEntityManager _entityManager;
        private readonly ISerializationManager _serializationManager;

        public Dictionary<(Type, Type), object> TypeReaders { get; }
        public Dictionary<Type, object> TypeWriters { get; }
        public Dictionary<Type, object> TypeCopiers => TypeWriters;
        public Dictionary<(Type, Type), object> TypeValidators => TypeReaders;

        // Run-specific data
        public Dictionary<ushort, string>? TileMap;
        public readonly Dictionary<string, MappingDataNode> CurrentReadingEntityComponents = new();
        public string? CurrentWritingComponent;
        public EntityUid? CurrentWritingEntity;

        private Dictionary<int, EntityUid> _uidEntityMap = new();
        private Dictionary<EntityUid, int> _entityUidMap = new();

        public MapSerializationContext(IComponentFactory factory, IEntityManager entityManager, ISerializationManager serializationManager)
        {
            _factory = factory;
            _entityManager = entityManager;
            _serializationManager = serializationManager;

            TypeWriters = new Dictionary<Type, object>()
            {
                {typeof(EntityUid), this}
            };
            TypeReaders = new Dictionary<(Type, Type), object>()
            {
                {(typeof(EntityUid), typeof(ValueDataNode)), this}
            };
        }

        public void Set(Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
        {
            _uidEntityMap = uidEntityMap;
            _entityUidMap = entityUidMap;
        }

        public void Clear()
        {
            CurrentReadingEntityComponents?.Clear();
            CurrentWritingComponent = null;
            CurrentWritingEntity = null;
        }

        // Create custom object serializers that will correctly allow data to be overriden by the map file.
        MappingDataNode IEntityLoadContext.GetComponentData(string componentName,
            MappingDataNode? protoData)
        {
            if (CurrentReadingEntityComponents == null)
            {
                throw new InvalidOperationException();
            }


            if (CurrentReadingEntityComponents.TryGetValue(componentName, out var mapping))
            {
                if (protoData == null) return mapping.Copy();

                return _serializationManager.PushCompositionWithGenericNode(
                    _factory.GetRegistration(componentName).Type, new[] { protoData }, mapping, this);
            }

            return protoData ?? new MappingDataNode();
        }

        public IEnumerable<string> GetExtraComponentTypes()
        {
            return CurrentReadingEntityComponents!.Keys;
        }

        ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
            ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            if (node.Value == "null")
            {
                return new ValidatedValueNode(node);
            }

            if (!int.TryParse(node.Value, out var val) || !_uidEntityMap.ContainsKey(val))
            {
                return new ErrorNode(node, "Invalid EntityUid", true);
            }

            return new ValidatedValueNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, EntityUid value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            if (!_entityUidMap.TryGetValue(value, out var entityUidMapped))
            {
                // Terrible hack to mute this warning on the grids themselves when serializing blueprints.
                if (CurrentWritingComponent != "Transform")
                {
                    Logger.WarningS("map", "Cannot write entity UID '{0}'.", value);
                }

                return new ValueDataNode("null");
            }

            return new ValueDataNode(entityUidMapped.ToString(CultureInfo.InvariantCulture));
        }

        EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context, EntityUid _)
        {
            if (node.Value == "null")
            {
                return EntityUid.Invalid;
            }

            var val = int.Parse(node.Value);

            if (!_uidEntityMap.TryGetValue(val, out var entity))
            {
                Logger.ErrorS("map", "Error in map file: found local entity UID '{0}' which does not exist.", val);
                return EntityUid.Invalid;
            }
            else
            {
                return entity;
            }
        }

        [MustUseReturnValue]
        public EntityUid Copy(ISerializationManager serializationManager, EntityUid source, EntityUid target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new((int)source);
        }
    }

    /// <summary>
    ///     Does basic pre-deserialization checks on map file load.
    ///     For example, let's not try to use maps with multiple grids as blueprints, shall we?
    /// </summary>
    private sealed class MapData
    {
        public YamlStream Stream { get; }

        public YamlNode RootNode => Stream.Documents[0].RootNode;

        public MappingDataNode RootMappingNode { get; }

        public readonly MapId TargetMap;
        public bool MapIsPostInit;
        public bool MapIsPaused;
        public readonly MapLoadOptions Options;

        // Loading data
        public readonly List<EntityUid> Entities = new();
        public readonly Dictionary<int, EntityUid> UidEntityMap = new();
        public readonly Dictionary<EntityUid, MappingDataNode> EntitiesToDeserialize = new();

        public readonly Dictionary<EntityUid, EntityUid> Hierarchy = new();
        public List<EntityUid> InitOrder = default!;

        public MapData(MapId mapId, TextReader reader, MapLoadOptions options)
        {
            var stream = new YamlStream();
            stream.Load(reader);

            if (stream.Documents.Count < 1)
            {
                throw new InvalidDataException("Stream has no YAML documents.");
            }

            // Kinda wanted to just make this print a warning and pick [0] but screw that.
            // What is this, a hug box?
            if (stream.Documents.Count > 1)
            {
                throw new InvalidDataException("Stream too many YAML documents. Map files store exactly one.");
            }

            Stream = stream;
            RootMappingNode = RootNode.ToDataNodeCast<MappingDataNode>();
            Options = options;
            TargetMap = mapId;
        }
    }
}
