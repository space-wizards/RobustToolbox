using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Physics;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps
{
    /// <summary>
    ///     Saves and loads maps to the disk.
    /// </summary>
    public sealed class MapLoader : IMapLoader
    {
        private static readonly MapLoadOptions DefaultLoadOptions = new();

        private const int MapFormatVersion = 2;

        [Dependency] private readonly IResourceManager _resMan = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IServerEntityManagerInternal _serverEntityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public event Action<YamlStream, string>? LoadedMapData;

        /// <inheritdoc />
        public void SaveGrid(EntityUid gridId, string yamlPath)
        {

            var grid = _mapManager.GetGrid(gridId);

            // Dont save grid fixtures.
            if (_serverEntityManager.TryGetComponent(grid.GridEntityId, out FixturesComponent? fixtures))
            {
                fixtures.SerializedFixtureData = new(); // empty list.
            }
            var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _prototypeManager, _serializationManager, _componentFactory);
            context.RegisterGrid(grid);
            var root = context.Serialize();
            var document = new YamlDocument(root);

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using var writer = _resMan.UserData.OpenWriteText(resPath);

            var stream = new YamlStream();
            stream.Add(document);
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        /// <inheritdoc />
        public (IReadOnlyList<EntityUid> entities, EntityUid? gridId) LoadGrid(MapId mapId, string path)
        {
            return LoadGrid(mapId, path, DefaultLoadOptions);
        }

        private ResourcePath Rooted(string path)
        {
            return new ResourcePath(path).ToRootedPath();
        }

        public (IReadOnlyList<EntityUid> entities, EntityUid? gridId) LoadGrid(MapId mapId, string path, MapLoadOptions options)
        {
            DebugTools.Assert(_mapManager.MapExists(mapId));

            var oldLoadMapOpt = options.LoadMap; // lets not mutate the default options
            options.LoadMap = false;

            var resPath = Rooted(path);

            if (!TryGetReader(resPath, out var reader)) return (Array.Empty<EntityUid>(), null);

            IMapGrid? grid;
            IReadOnlyList<EntityUid> entities;
            using (reader)
            {
                Logger.InfoS("map", $"Loading Grid: {resPath}");

                var data = new MapData(reader);

                LoadedMapData?.Invoke(data.Stream, resPath.ToString());

                if (data.GridCount != 1)
                {
                    throw new InvalidDataException("Cannot instance map with multiple grids as blueprint.");
                }

                var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager,
                    _prototypeManager, _serializationManager, _componentFactory, data.RootNode.ToDataNodeCast<MappingDataNode>(), mapId, options);
                context.LogErrorOnMap = true;
                context.Deserialize();
                grid = context.Grids.FirstOrDefault();
                entities = context.Entities;

                PostDeserialize(mapId, context);
                options.LoadMap = oldLoadMapOpt;
            }

            return (entities, grid?.GridEntityId);
        }

        private void PostDeserialize(MapId mapId, MapContext context)
        {
            var isPaused = _mapManager.IsMapPaused(mapId);
            var query = _serverEntityManager.GetEntityQuery<MetaDataComponent>();
            var metaSystem = _serverEntityManager.EntitySysManager.GetEntitySystem<MetaDataSystem>();

            if (context.MapIsPostInit)
            {
                foreach (var entity in context.Entities)
                {
                    query.GetComponent(entity).EntityLifeStage = EntityLifeStage.MapInitialized;
                }
            }
            else if (_mapManager.IsMapInitialized(mapId))
            {
                foreach (var entity in context.Entities)
                {
                    var meta = query.GetComponent(entity);
                    _serverEntityManager.RunMapInit(entity, meta);
                    if (isPaused)
                        metaSystem.SetEntityPaused(entity, true, meta);
                }
            }
            else if (isPaused)
            {
                foreach (var entity in context.Entities)
                {
                    var meta = query.GetComponent(entity);
                    metaSystem.SetEntityPaused(entity, true, meta);
                }
            }
        }

        /// <inheritdoc />
        public void SaveMap(MapId mapId, string yamlPath)
        {
            Logger.InfoS("map", $"Saving map {mapId} to {yamlPath}");
            var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _prototypeManager, _serializationManager, _componentFactory);
            context.MapId = mapId;

            foreach (var grid in _mapManager.GetAllMapGrids(mapId))
            {
                // Dont save grid fixtures.
                if (_serverEntityManager.TryGetComponent(grid.GridEntityId, out FixturesComponent? fixtures))
                {
                    fixtures.SerializedFixtureData = new(); // empty list.
                }
                context.RegisterGrid(grid);
            }

            var document = new YamlDocument(context.Serialize());

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using var writer = _resMan.UserData.OpenWriteText(resPath);

            var stream = new YamlStream();
            stream.Add(document);
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);

            Logger.InfoS("map", "Save completed!");
        }

        public (IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> gridIds) LoadMap(MapId mapId, string path)
        {
            return LoadMap(mapId, path, DefaultLoadOptions);
        }

        private bool TryGetReader(ResourcePath resPath, [NotNullWhen(true)] out TextReader? reader)
        {
            // try user
            if (!_resMan.UserData.Exists(resPath))
            {
                Logger.InfoS("map", $"No user map found: {resPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(resPath, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No map found: {resPath}");
                    reader = null;
                    return false;
                }
            }
            else
            {
                reader = _resMan.UserData.OpenText(resPath);
            }

            return true;
        }

        public (IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> gridIds) LoadMap(MapId mapId, string path, MapLoadOptions options)
        {
            var resPath = Rooted(path);

            if (!TryGetReader(resPath, out var reader)) return (Array.Empty<EntityUid>(), Array.Empty<EntityUid>());

            IReadOnlyList<EntityUid> grids;
            IReadOnlyList<EntityUid> entities;
            using (reader)
            {
                Logger.InfoS("map", $"Loading Map: {resPath}");

                var data = new MapData(reader);

                LoadedMapData?.Invoke(data.Stream, resPath.ToString());

                var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager,
                    _prototypeManager, _serializationManager, _componentFactory, data.RootNode.ToDataNodeCast<MappingDataNode>(), mapId, options);
                context.Deserialize();
                grids = context.Grids.Select(x => x.GridEntityId).ToArray(); // TODO: make context use grid IDs.
                entities = context.Entities;

                PostDeserialize(mapId, context);
            }

            return (entities, grids);
        }

        /// <summary>
        ///     Handles the primary bulk of state during the map serialization process.
        /// </summary>
        internal sealed class MapContext : ISerializationContext, IEntityLoadContext,
            ITypeSerializer<EntityUid, ValueDataNode>,
            ITypeReaderWriter<EntityUid, ValueDataNode>
        {
            private readonly IMapManagerInternal _mapManager;
            private readonly ITileDefinitionManager _tileDefinitionManager;
            private readonly IServerEntityManagerInternal _serverEntityManager;
            private readonly IPrototypeManager _prototypeManager;
            private readonly ISerializationManager _serializationManager;
            private readonly IComponentFactory _componentFactory;

            private readonly MapLoadOptions? _loadOptions;

            /// <summary>
            /// If we're using savemap and not savebp then save everything on map.
            /// </summary>
            internal MapId? MapId { get; set; }
            private readonly Dictionary<EntityUid, int> GridIDMap = new();
            public readonly List<MapGrid> Grids = new();
            private EntityQuery<TransformComponent>? _xformQuery = null;

            private readonly Dictionary<EntityUid, int> EntityUidMap = new();
            private readonly Dictionary<int, EntityUid> UidEntityMap = new();
            public readonly List<EntityUid> Entities = new();

            private readonly List<(EntityUid, MappingDataNode)> _entitiesToDeserialize
                = new();

            /// <summary>
            /// If true, this will log an error when encountering a map entity. E.g., when using the loadgrid command to load a map file.
            /// </summary>
            public bool LogErrorOnMap = false;

            private bool IsBlueprintMode => GridIDMap.Count == 1;

            private readonly MappingDataNode RootNode;
            public readonly MapId TargetMap;
            private EntityUid? TargetMapUid;

            private Dictionary<string, MappingDataNode>? CurrentReadingEntityComponents;
            private HashSet<string>? CurrentlyIgnoredComponents;

            private string? CurrentWritingComponent;
            private EntityUid? CurrentWritingEntity;

            public IReadOnlyDictionary<ushort, string>? TileMap => _tileMap;
            private Dictionary<ushort, string>? _tileMap;

            public Dictionary<(Type, Type), object> TypeReaders { get; }
            public Dictionary<Type, object> TypeWriters { get; }
            public Dictionary<Type, object> TypeCopiers => TypeWriters;
            public Dictionary<(Type, Type), object> TypeValidators => TypeReaders;

            public bool MapIsPostInit { get; private set; }

            public MapContext(IMapManagerInternal maps, ITileDefinitionManager tileDefs,
                IServerEntityManagerInternal entities, IPrototypeManager prototypeManager,
                ISerializationManager serializationManager, IComponentFactory componentFactory)
            {
                _mapManager = maps;
                _tileDefinitionManager = tileDefs;
                _serverEntityManager = entities;
                _prototypeManager = prototypeManager;
                _serializationManager = serializationManager;
                _componentFactory = componentFactory;

                RootNode = new MappingDataNode();
                TypeWriters = new Dictionary<Type, object>()
                {
                    {typeof(EntityUid), this}
                };
                TypeReaders = new Dictionary<(Type, Type), object>()
                {
                    {(typeof(EntityUid), typeof(ValueDataNode)), this}
                };
            }

            public MapContext(IMapManagerInternal maps, ITileDefinitionManager tileDefs,
                IServerEntityManagerInternal entities,
                IPrototypeManager prototypeManager,
                ISerializationManager serializationManager,
                IComponentFactory componentFactory,
                MappingDataNode node, MapId targetMapId, MapLoadOptions options)
            {
                _mapManager = maps;
                _tileDefinitionManager = tileDefs;
                _serverEntityManager = entities;
                _loadOptions = options;
                _serializationManager = serializationManager;
                _componentFactory = componentFactory;

                RootNode = node;
                TargetMap = targetMapId;
                _prototypeManager = prototypeManager;
                TypeWriters = new Dictionary<Type, object>()
                {
                    {typeof(EntityUid), this}
                };
                TypeReaders = new Dictionary<(Type, Type), object>()
                {
                    {(typeof(EntityUid), typeof(ValueDataNode)), this}
                };
            }

            // Deserialization
            public void Deserialize()
            {
                // Verify that prototypes for all the entities exist and throw if they don't.
                VerifyEntitiesExist();

                // First we load map meta data like version.
                ReadMetaSection();

                // Create the new map.
                AllocMap();

                // Entities are first allocated. This allows us to know the future UID of all entities on the map before
                // even ExposeData is loaded. This allows us to resolve serialized EntityUid instances correctly.
                AllocEntities();

                // Actually instance components and run ExposeData on them.
                FinishEntitiesLoad();

                // Load grids.
                ReadTileMapSection();

                // Reads the grid section, allocates MapGrids, and maps them to their respective MapGridComponents.
                ReadGridSection();

                // Clear the net tick numbers so that components from prototypes (not modified by map)
                // aren't sent over the wire initially.
                ResetNetTicks();

                // Grid entities were NOT created inside ReadGridSection().
                // We have to fix the created grids up with the grid entities deserialized from the map.
                FixMapEntities();

                _xformQuery = _serverEntityManager.GetEntityQuery<TransformComponent>();

                // We have to attach grids to the target map here.
                // If we don't, initialization & startup can fail for some entities.
                AttachMapEntities();

                ApplyGridFixtures();

                AdjustEntityTransforms();

                // Run Initialize on all components.
                FinishEntitiesInitialization();

                // Run Startup on all components.
                FinishEntitiesStartup();

                // Do this last so any entity transforms are fixed first and that they go to the new grids correctly.
                CheckGridSplits();
            }

            private void VerifyEntitiesExist()
            {
                var fail = false;
                var entities = RootNode.Get<SequenceDataNode>("entities");
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

                if (fail)
                {
                    throw new InvalidOperationException(
                        "Found missing prototypes in map file. Missing prototypes have been dumped to logs.");
                }
            }

            private void ResetNetTicks()
            {
                var compFactory = IoCManager.Resolve<IComponentFactory>();

                foreach (var (entity, data) in _entitiesToDeserialize)
                {
                    if (!data.TryGet("components", out SequenceDataNode? componentList))
                    {
                        continue;
                    }

                    if (_serverEntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype is not {} prototype)
                    {
                        continue;
                    }

                    foreach (var (netId, component) in _serverEntityManager.GetNetComponents(entity))
                    {
                        var castComp = (Component) component;
                        var compName = compFactory.GetComponentName(castComp.GetType());

                        if (componentList.Cast<MappingDataNode>().Any(p => ((ValueDataNode)p["type"]).Value == compName))
                        {
                            if (prototype.Components.ContainsKey(compName))
                            {
                                // This component is modified by the map so we have to send state.
                                // Though it's still in the prototype itself so creation doesn't need to be sent.
                                castComp.ClearCreationTick();
                            }

                            continue;
                        }

                        // This component is not modified by the map file,
                        // so the client will have the same data after instantiating it from prototype ID.
                        castComp.ClearTicks();
                    }
                }
            }

            /// <summary>
            /// Go through all of the queued chunks that need updating and make sure their bounds are set.
            /// </summary>
            private void ApplyGridFixtures()
            {
                var entManager = _serverEntityManager;
                var sysManager = _serverEntityManager.EntitySysManager;
                var gridFixtures = sysManager.GetEntitySystem<GridFixtureSystem>();
                var fixtureSystem = sysManager.GetEntitySystem<FixtureSystem>();
                // Disable splitting temporarily while maploading occurs.
                var splitEnabled = gridFixtures.SplitAllowed;
                gridFixtures.SplitAllowed = false;

                foreach (var grid in Grids)
                {
                    var gridInternal = (IMapGridInternal) grid;
                    var body = entManager.EnsureComponent<PhysicsComponent>(grid.GridEntityId);
                    var fixtures = entManager.EnsureComponent<FixturesComponent>(grid.GridEntityId);
                    // Regenerate grid collision.
                    gridFixtures.EnsureGrid(grid.GridEntityId);
                    gridFixtures.ProcessGrid(grid);
                    // Avoid duplicating the deserialization in FixtureSystem.
                    fixtures.SerializedFixtureData = null;

                    // Need to go through and double-check we don't have any hanging-on fixtures that
                    // no longer apply (e.g. due to an update in GridFixtureSystem)
                    var toRemove = new RemQueue<Fixture>();

                    foreach (var (_, fixture) in fixtures.Fixtures)
                    {
                        var found = false;

                        foreach (var (_, chunk) in gridInternal.GetMapChunks())
                        {
                            foreach (var cFixture in chunk.Fixtures)
                            {
                                if (!cFixture.Equals(fixture)) continue;
                                found = true;
                                break;
                            }

                            if (found) break;
                        }

                        if (!found)
                        {
                            toRemove.Add(fixture);
                        }
                    }

                    foreach (var fixture in toRemove)
                    {
                        fixtureSystem.DestroyFixture(body, fixture, false, fixtures);
                    }

                    fixtureSystem.FixtureUpdate(fixtures, body);
                }

                gridFixtures.SplitAllowed = splitEnabled;
            }

            private void ReadGridSection()
            {
                // MapGrids already contain their assigned GridId from their ctor, and the MapComponents just got deserialized.
                // Now we need to actually bind the MapGrids to their components so that you can resolve GridId -> EntityUid
                // After doing this, it should be 100% safe to use the MapManager API like normal.

                var yamlGrids = RootNode.Get<SequenceDataNode>("grids");

                // There were no new grids, nothing to do here.
                if (yamlGrids.Count == 0)
                    return;

                // get ents that the grids will bind to
                var gridComps = new MapGridComponent[yamlGrids.Count];

                var gridQuery = _serverEntityManager.GetEntityQuery<MapGridComponent>();

                // linear search for new grid comps
                foreach (var tuple in _entitiesToDeserialize)
                {
                    if (!gridQuery.TryGetComponent(tuple.Item1, out var gridComp))
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
                        var (chunkOffsetX, chunkOffsetY) = _serializationManager.Read<Vector2i>(chunkNode["ind"]);
                        var chunk = grid.GetChunk(chunkOffsetX, chunkOffsetY);
                        _serializationManager.Read(chunkNode, this, value: chunk);
                    }

                    Grids.Add(grid); // Grids are kept in index order
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

            private void AttachMapEntities()
            {
                EntityUid mapEntity;

                if (TargetMapUid != null)
                {
                    mapEntity = TargetMapUid.Value;
                    _mapManager.SetMapEntity(TargetMap, TargetMapUid.Value);
                }
                else
                {
                    mapEntity = _mapManager.GetMapEntityIdOrThrow(TargetMap);
                }

                foreach (var grid in Grids)
                {
                    var transform = _xformQuery!.Value.GetComponent(grid.GridEntityId);
                    if (transform.MapUid?.IsValid() == true)
                        continue;

                    var mapOffset = transform.LocalPosition;
                    transform.AttachParent(mapEntity);
                    transform.WorldPosition = mapOffset;
                }
            }

            private void FixMapEntities()
            {
                var pvs = EntitySystem.Get<PVSSystem>();

                foreach (var grid in Grids)
                {
                    pvs?.EntityPVSCollection.UpdateIndex(grid.GridEntityId);
                    // The problem here is that the grid is initialising at the same time as everything else which
                    // is bad for slothcoin because a bunch of components are only added
                    // to the grid during its initialisation hence you get exceptions
                    // hence this 1 snowflake thing.
                    _serverEntityManager.EnsureComponent<BroadphaseComponent>(grid.GridEntityId);
                }
            }

            private void ReadMetaSection()
            {
                var meta = RootNode.Get<MappingDataNode>("meta");
                var ver = meta.Get<ValueDataNode>("format").AsInt();
                if (ver != MapFormatVersion)
                {
                    throw new InvalidDataException("Cannot handle this map file version.");
                }

                if (meta.TryGet<ValueDataNode>("postmapinit", out var mapInitNode))
                {
                    MapIsPostInit = mapInitNode.AsBool();
                }
                else
                {
                    MapIsPostInit = true;
                }
            }

            private void ReadTileMapSection()
            {
                // Load tile mapping so that we can map the stored tile IDs into the ones actually used at runtime.
                _tileMap = new Dictionary<ushort, string>();

                var tileMap = RootNode.Get<MappingDataNode>("tilemap");
                foreach (var (key, value) in tileMap.Children)
                {
                    var tileId = (ushort) ((ValueDataNode)key).AsInt();
                    var tileDefName = ((ValueDataNode)value).Value;
                    _tileMap.Add(tileId, tileDefName);
                }
            }

            private void AllocMap()
            {
                // Both blueprint and map deserialization use this,
                // so we need to ensure the map exists (and the map entity)
                // before allocating entities.

                if (!_mapManager.MapExists(TargetMap))
                {
                    _mapManager.CreateMap(TargetMap);

                    if (!MapIsPostInit)
                    {
                        _mapManager.AddUninitializedMap(TargetMap);
                    }
                }
            }

            private void AllocEntities()
            {
                var entities = RootNode.Get<SequenceDataNode>("entities");
                Entities.EnsureCapacity(entities.Count);
                UidEntityMap.EnsureCapacity(entities.Count);
                _entitiesToDeserialize.EnsureCapacity(entities.Count);

                foreach (var entityDef in entities.Cast<MappingDataNode>())
                {
                    string? type = null;
                    if (entityDef.TryGet<ValueDataNode>("type", out var typeNode))
                    {
                        type = typeNode.Value;
                    }

                    // TODO Fix this. If the entities are ever defined out of order, and if one of them does not have a
                    // "uid" node, then defaulting to Entities.Count will error.
                    var uid = Entities.Count;
                    if (entityDef.TryGet<ValueDataNode>("uid", out var uidNode))
                    {
                        uid = uidNode.AsInt();
                    }

                    var entity = _serverEntityManager.AllocEntity(type);
                    Entities.Add(entity);
                    UidEntityMap.Add(uid, entity);
                    _entitiesToDeserialize.Add((entity, entityDef));

                    if (_loadOptions!.StoreMapUids)
                    {
                        var comp = _serverEntityManager.AddComponent<MapSaveIdComponent>(entity);
                        comp.Uid = uid;
                    }
                }
            }

            private void FinishEntitiesLoad()
            {
                var mapQuery = _serverEntityManager.GetEntityQuery<MapComponent>();
                var metaQuery = _serverEntityManager.GetEntityQuery<MetaDataComponent>();

                foreach (var (entity, data) in _entitiesToDeserialize)
                {
                    CurrentReadingEntityComponents = new Dictionary<string, MappingDataNode>();
                    HashSet<string> missingComponents = new();
                    if (data.TryGet("components", out SequenceDataNode? componentList))
                    {
                        foreach (var compData in componentList.Cast<MappingDataNode>())
                        {
                            var datanode = compData.Copy();
                            datanode.Remove("type");
                            var value = ((ValueDataNode)compData["type"]).Value;
                            CurrentReadingEntityComponents[value] = datanode;
                        }
                    }

                    if (data.TryGet("missingComponents", out SequenceDataNode? missingComponentList))
                        CurrentlyIgnoredComponents = missingComponentList.Cast<ValueDataNode>().Select(x => x.Value).ToHashSet();
                    else
                        CurrentlyIgnoredComponents = new();

                    _serverEntityManager.FinishEntityLoad(entity, metaQuery.GetComponent(entity).EntityPrototype, this);

                    if (!mapQuery.HasComponent(entity))
                        continue;

                    if (LogErrorOnMap)
                        Logger.ErrorS("map", "Found an additional map entity while loading a map/grid. Either you are using loadgrid to load a map file, or your map file contains more than one map entity.");

                    if ((_loadOptions?.LoadMap ?? true) && TargetMapUid == null)
                    {
                        TargetMapUid = entity;

                        // error on any additional map entities.
                        LogErrorOnMap = true;
                    }
                }
            }

            private void AdjustEntityTransforms()
            {
                var map = _mapManager.GetMapEntityId(TargetMap);

                if (_loadOptions is null || _loadOptions.TransformMatrix.EqualsApprox(Matrix3.Identity))
                    return;

                var xformSys = _serverEntityManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

                foreach (var entity in Entities)
                {
                    if (!_xformQuery!.Value.TryGetComponent(entity, out var transform) ||
                        transform.ParentUid != map) continue;

                    var off = _loadOptions.TransformMatrix.Transform(transform.Coordinates.Position);

                    xformSys.SetCoordinates(transform, transform.Coordinates.WithPosition(off));
                    transform.WorldRotation += _loadOptions.Rotation;
                }
            }

            private void FinishEntitiesInitialization()
            {
                // Ideally MapLoader would just be topdown and I could just set the root to null instead
                // then we'd have a nice clean init, but instead it's done per stage and we need to make sure it gets
                // handled per stage.
                var query = _serverEntityManager.GetEntityQuery<MetaDataComponent>();
                var mapQuery = _serverEntityManager.GetEntityQuery<MapComponent>();
                var failure = false;

                for (var i = 0; i < Entities.Count; i++)
                {
                    var entity = Entities[i];

                    // If we're loading a map but not 'loading the map' then kill it
                    if (TargetMapUid == null && mapQuery.HasComponent(entity))
                    {
                        Logger.InfoS("map", $"Deleting extra map entity: {_serverEntityManager.ToPrettyString(entity)}");
                        _serverEntityManager.DeleteEntity(entity);
                        Entities.RemoveSwap(i);
                        _entitiesToDeserialize.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (!query.TryGetComponent(entity, out var meta))
                    {
                        Logger.Error("map", $"Found deleted entity {entity} (original uid {_entitiesToDeserialize[i].Item2[0].Value}) on maploader!");
                        failure = true;
                        continue;
                    }

                    _serverEntityManager.FinishEntityInitialization(entity, meta);
                }

                if (failure)
                {
                    for (var i = 0; i < Entities.Count; i++)
                    {
                        _serverEntityManager.DeleteEntity(Entities[i]);
                    }

                    Entities.Clear();
                    _entitiesToDeserialize.Clear();

                    throw new InvalidOperationException(
                        $"Failed to load map {TargetMap} due to deleted entities, see log for info");
                }
            }

            private void FinishEntitiesStartup()
            {
                foreach (var entity in Entities)
                {
                    _serverEntityManager.FinishEntityStartup(entity);
                }
            }

            private void CheckGridSplits()
            {
                var gridFixtures = _serverEntityManager.EntitySysManager.GetEntitySystem<GridFixtureSystem>();
                foreach (var grid in Grids)
                {
                    if (_serverEntityManager.Deleted(grid.GridEntityId)) continue;
                    gridFixtures.CheckSplits(grid.GridEntityId);
                }
            }

            // Serialization
            public void RegisterGrid(IMapGrid grid)
            {
                if (GridIDMap.ContainsKey(grid.GridEntityId))
                {
                    throw new InvalidOperationException();
                }

                Grids.Add((MapGrid) grid);
                GridIDMap.Add(grid.GridEntityId, GridIDMap.Count);
            }

            public YamlNode Serialize()
            {
                WriteMetaSection();
                WriteTileMapSection();
                PopulateEntityList();
                WriteGridSection();
                WriteEntitySection();

                return RootNode.ToYaml();
            }

            private void WriteMetaSection()
            {
                var meta = new MappingDataNode();
                RootNode.Add("meta", meta);
                meta.Add("format", MapFormatVersion.ToString(CultureInfo.InvariantCulture));
                // TODO: Make these values configurable.
                meta.Add("name", "DemoStation");
                meta.Add("author", "Space-Wizards");

                //TODO: MapId is null when saveBP is used, another reason this jumbled mess needs to be rewritten
                var isPostInit = MapId is not null && _mapManager.IsMapInitialized(MapId.Value);

                //TODO: This is a workaround to make SaveBP function
                foreach (var grid in Grids)
                {
                    var mapId = _serverEntityManager.GetComponent<TransformComponent>(grid.GridEntityId).MapID;

                    if (_mapManager.IsMapInitialized(mapId))
                    {
                        isPostInit = true;
                        break;
                    }
                }

                meta.Add("postmapinit", isPostInit ? "true" : "false");
            }

            private void WriteTileMapSection()
            {
                var tileMap = new MappingDataNode();
                RootNode.Add("tilemap", tileMap);
                foreach (var tileDefinition in _tileDefinitionManager)
                {
                    tileMap.Add(tileDefinition.TileId.ToString(CultureInfo.InvariantCulture), tileDefinition.ID);
                }
            }

            private void WriteGridSection()
            {
                var grids = new SequenceDataNode();
                RootNode.Add("grids", grids);

                int index = 0;
                foreach (var grid in Grids)
                {
                    _serverEntityManager.GetComponent<MapGridComponent>(grid.GridEntityId).GridIndex = index;
                    var entry = _serializationManager.WriteValue(grid, context: this);
                    grids.Add(entry);
                    index++;
                }
            }

            private void PopulateEntityList()
            {
                var withoutUid = new HashSet<EntityUid>();
                var saveCompQuery = _serverEntityManager.GetEntityQuery<MapSaveIdComponent>();
                var transformCompQuery = _serverEntityManager.GetEntityQuery<TransformComponent>();
                var metaCompQuery = _serverEntityManager.GetEntityQuery<MetaDataComponent>();
                foreach (var entity in _serverEntityManager.GetEntities())
                {
                    var currentTransform = transformCompQuery.GetComponent(entity);
                    if (MapId != null && currentTransform.MapID != MapId) continue;
                    if (MapId == null && (!(currentTransform.GridUid is EntityUid gridId) || !GridIDMap.ContainsKey(gridId))) continue;

                    var currentEntity = entity;

                    // Don't serialize things parented to un savable things.
                    // For example clothes inside a person.
                    while (currentEntity.IsValid())
                    {
                        if (metaCompQuery.GetComponent(currentEntity).EntityPrototype?.MapSavable == false) break;
                        currentEntity = transformCompQuery.GetComponent(currentEntity).ParentUid;
                    }

                    if (currentEntity.IsValid()) continue;

                    Entities.Add(entity);

                    if (!saveCompQuery.TryGetComponent(entity, out var mapSaveComp) ||
                        !UidEntityMap.TryAdd(mapSaveComp.Uid, entity))
                    {
                        // If the id was already saved before, or has no save component we need to find a new id for this entity
                        withoutUid.Add(entity);
                    }
                }

                var uidCounter = 0;
                foreach (var entity in withoutUid)
                {
                    while (UidEntityMap.ContainsKey(uidCounter))
                    {
                        // Find next available UID.
                        uidCounter += 1;
                    }

                    UidEntityMap.Add(uidCounter, entity);
                    uidCounter += 1;
                }

                // Build a reverse lookup
                EntityUidMap.EnsureCapacity(UidEntityMap.Count);
                foreach(var (saveId, mapId) in UidEntityMap)
                {
                    EntityUidMap.Add(mapId, saveId);
                }
            }

            private void WriteEntitySection()
            {
                var serializationManager = IoCManager.Resolve<ISerializationManager>();
                var compFactory = IoCManager.Resolve<IComponentFactory>();
                var metaQuery = _serverEntityManager.GetEntityQuery<MetaDataComponent>();
                var entities = new SequenceDataNode();
                RootNode.Add("entities", entities);

                var prototypeCompCache = new Dictionary<string, Dictionary<string, MappingDataNode>>();
                foreach (var (saveId, entityUid) in UidEntityMap.OrderBy(e=>e.Key))
                {
                    CurrentWritingEntity = entityUid;
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
                            prototypeCompCache[prototype.ID] = cache =  new Dictionary<string, MappingDataNode>();
                            foreach (var (compType, comp) in prototype.Components)
                            {
                                cache.Add(compType, serializationManager.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component));
                            }
                        }
                    }

                    var components = new SequenceDataNode();

                    // See engine#636 for why the Distinct() call.
                    foreach (var component in _serverEntityManager.GetComponents(entityUid))
                    {
                        if (component is MapSaveIdComponent)
                            continue;

                        var compType = component.GetType();
                        var compName = compFactory.GetComponentName(compType);
                        CurrentWritingComponent = compName;
                        var compMapping = serializationManager.WriteValueAs<MappingDataNode>(compType, component, context: this);

                        if (cache != null && cache.TryGetValue(compName, out var protMapping))
                        {
                            // This will NOT recursively call Except() on the values of the mapping. It will only remove
                            // key-value pairs if both the keys and values are equal.
                            compMapping = compMapping.Except(protMapping);
                            if(compMapping == null) continue;
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

                    if (md.EntityPrototype == null)
                    {
                        // No prototype - we are done.
                        entities.Add(mapping);
                        return;
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
                        _componentFactory.GetRegistration(componentName).Type, new[] { protoData }, mapping, this);
                }

                return protoData ?? new MappingDataNode();
            }

            public IEnumerable<string> GetExtraComponentTypes()
            {
                return CurrentReadingEntityComponents!.Keys;
            }

            public bool ShouldSkipComponent(string compName)
            {
                return CurrentlyIgnoredComponents!.Contains(compName);
            }

            [Virtual]
            public class MapLoadException : Exception
            {
                public MapLoadException(string? message)
                    : base(message) { }
            }

            ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
                ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
            {
                if (node.Value == "null")
                {
                    return new ValidatedValueNode(node);
                }

                if (!int.TryParse(node.Value, out var val) || !UidEntityMap.ContainsKey(val))
                {
                    return new ErrorNode(node, "Invalid EntityUid", true);
                }

                return new ValidatedValueNode(node);
            }

            public DataNode Write(ISerializationManager serializationManager, EntityUid value,
                IDependencyCollection dependencies, bool alwaysWrite = false,
                ISerializationContext? context = null)
            {
                if (!EntityUidMap.TryGetValue(value, out var entityUidMapped))
                {
                    // Terrible hack to mute this warning on the grids themselves when serializing blueprints.
                    if (!IsBlueprintMode || !_serverEntityManager.HasComponent<MapGridComponent>(CurrentWritingEntity!.Value) ||
                        CurrentWritingComponent != "Transform")
                    {
                        Logger.WarningS("map", "Cannot write entity UID '{0}'.", value);
                    }

                    return new ValueDataNode("null");
                }
                else
                {
                    return new ValueDataNode(entityUidMapped.ToString(CultureInfo.InvariantCulture));
                }
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

                if (!UidEntityMap.TryGetValue(val, out var entity))
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
                return new((int) source);
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
            public int GridCount { get; }

            public MapData(TextReader reader)
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
                GridCount = ((YamlSequenceNode) RootNode["grids"]).Children.Count;
            }
        }
    }
}
