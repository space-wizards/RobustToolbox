using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.YAML;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps
{
    /// <summary>
    ///     Saves and loads maps to the disk.
    /// </summary>
    public class MapLoader : IMapLoader
    {
        private static readonly MapLoadOptions DefaultLoadOptions = new();

        private const int MapFormatVersion = 2;

        [Dependency] private readonly IResourceManager _resMan = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IServerEntityManagerInternal _serverEntityManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public event Action<YamlStream, string>? LoadedMapData;

        /// <inheritdoc />
        public void SaveBlueprint(GridId gridId, string yamlPath)
        {
            var grid = _mapManager.GetGrid(gridId);

            var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager,
                _componentManager, _prototypeManager);
            context.RegisterGrid(grid);
            var root = context.Serialize();
            var document = new YamlDocument(root);

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using (var file = _resMan.UserData.Create(resPath))
            {
                using (var writer = new StreamWriter(file))
                {
                    var stream = new YamlStream();

                    stream.Add(document);
                    stream.Save(new YamlMappingFix(new Emitter(writer)), false);
                }
            }
        }

        /// <inheritdoc />
        public IMapGrid? LoadBlueprint(MapId mapId, string path)
        {
            return LoadBlueprint(mapId, path, DefaultLoadOptions);
        }

        public IMapGrid? LoadBlueprint(MapId mapId, string path, MapLoadOptions options)
        {
            TextReader reader;
            var resPath = new ResourcePath(path).ToRootedPath();

            // try user
            if (!_resMan.UserData.Exists(resPath))
            {
                Logger.InfoS("map", $"No user blueprint path: {resPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(resPath, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No blueprint found: {resPath}");
                    return null;
                }
            }
            else
            {
                var file = _resMan.UserData.OpenRead(resPath);
                reader = new StreamReader(file);
            }

            IMapGrid grid;
            using (reader)
            {
                Logger.InfoS("map", $"Loading Grid: {resPath}");

                var data = new MapData(reader);

                LoadedMapData?.Invoke(data.Stream, resPath.ToString());

                if (data.GridCount != 1)
                {
                    throw new InvalidDataException("Cannot instance map with multiple grids as blueprint.");
                }

                var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager,
                    _componentManager, _prototypeManager, (YamlMappingNode) data.RootNode, mapId, options);
                context.Deserialize();
                grid = context.Grids[0];

                if (!context.MapIsPostInit && _pauseManager.IsMapInitialized(mapId))
                {
                    foreach (var entity in context.Entities)
                    {
                        entity.RunMapInit();
                    }
                }

                if (_pauseManager.IsMapPaused(mapId))
                {
                    foreach (var entity in context.Entities)
                    {
                        entity.Paused = true;
                    }
                }
            }

            return grid;
        }

        /// <inheritdoc />
        public void SaveMap(MapId mapId, string yamlPath)
        {
            Logger.InfoS("map", $"Saving map {mapId} to {yamlPath}");
            var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager,
                _componentManager, _prototypeManager);
            foreach (var grid in _mapManager.GetAllMapGrids(mapId))
            {
                context.RegisterGrid(grid);
            }

            var document = new YamlDocument(context.Serialize());

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using (var file = _resMan.UserData.Create(resPath))
            {
                using (var writer = new StreamWriter(file))
                {
                    var stream = new YamlStream();

                    stream.Add(document);
                    stream.Save(new YamlMappingFix(new Emitter(writer)), false);
                }
            }

            Logger.InfoS("map", "Save completed!");
        }

        public void LoadMap(MapId mapId, string path)
        {
            LoadMap(mapId, path, DefaultLoadOptions);
        }

        public void LoadMap(MapId mapId, string path, MapLoadOptions options)
        {
            TextReader reader;
            var resPath = new ResourcePath(path).ToRootedPath();

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
                    return;
                }
            }
            else
            {
                var file = _resMan.UserData.OpenRead(resPath);
                reader = new StreamReader(file);
            }

            using (reader)
            {
                Logger.InfoS("map", $"Loading Map: {resPath}");

                var data = new MapData(reader);

                LoadedMapData?.Invoke(data.Stream, resPath.ToString());

                var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager,
                    _componentManager, _prototypeManager, (YamlMappingNode) data.RootNode, mapId, options);
                context.Deserialize();

                if (!context.MapIsPostInit && _pauseManager.IsMapInitialized(mapId))
                {
                    foreach (var entity in context.Entities)
                    {
                        entity.RunMapInit();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the primary bulk of state during the map serialization process.
        /// </summary>
        private class MapContext : ISerializationContext, IEntityLoadContext,
            ITypeSerializer<GridId, ValueDataNode>,
            ITypeSerializer<EntityUid, ValueDataNode>,
            ITypeSerializer<IEntity, ValueDataNode>
        {
            private readonly IMapManagerInternal _mapManager;
            private readonly ITileDefinitionManager _tileDefinitionManager;
            private readonly IServerEntityManagerInternal _serverEntityManager;
            private readonly IPauseManager _pauseManager;
            private readonly IComponentManager _componentManager;
            private readonly IPrototypeManager _prototypeManager;

            private readonly MapLoadOptions? _loadOptions;
            private readonly Dictionary<GridId, int> GridIDMap = new();
            public readonly List<IMapGrid> Grids = new();

            private readonly Dictionary<EntityUid, int> EntityUidMap = new();
            private readonly Dictionary<int, EntityUid> UidEntityMap = new();
            public readonly List<IEntity> Entities = new();

            private readonly List<(IEntity, YamlMappingNode)> _entitiesToDeserialize
                = new();

            private bool IsBlueprintMode => GridIDMap.Count == 1;

            private readonly YamlMappingNode RootNode;
            private readonly MapId TargetMap;

            private Dictionary<string, YamlMappingNode>? CurrentReadingEntityComponents;

            private string? CurrentWritingComponent;
            private IEntity? CurrentWritingEntity;

            private Dictionary<ushort, string>? _tileMap;

            public bool MapIsPostInit { get; private set; }

            public Dictionary<Type, object> TypeSerializers { get; }

            public MapContext(IMapManagerInternal maps, ITileDefinitionManager tileDefs,
                IServerEntityManagerInternal entities, IPauseManager pauseManager, IComponentManager componentManager,
                IPrototypeManager prototypeManager)
            {
                _mapManager = maps;
                _tileDefinitionManager = tileDefs;
                _serverEntityManager = entities;
                _pauseManager = pauseManager;
                _componentManager = componentManager;
                _prototypeManager = prototypeManager;

                RootNode = new YamlMappingNode();
                TypeSerializers = new()
                {
                    {typeof(IEntity), this},
                    {typeof(GridId), this},
                    {typeof(EntityUid), this}
                };
            }

            public MapContext(IMapManagerInternal maps, ITileDefinitionManager tileDefs,
                IServerEntityManagerInternal entities,
                IPauseManager pauseManager, IComponentManager componentManager, IPrototypeManager prototypeManager,
                YamlMappingNode node, MapId targetMapId, MapLoadOptions options)
            {
                _mapManager = maps;
                _tileDefinitionManager = tileDefs;
                _serverEntityManager = entities;
                _pauseManager = pauseManager;
                _componentManager = componentManager;
                _loadOptions = options;

                RootNode = node;
                TargetMap = targetMapId;
                _prototypeManager = prototypeManager;
                TypeSerializers = new()
                {
                    {typeof(IEntity), this},
                    {typeof(GridId), this},
                    {typeof(EntityUid), this}
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

                // Load grids.
                ReadTileMapSection();
                ReadGridSection();

                // Entities are first allocated. This allows us to know the future UID of all entities on the map before
                // even ExposeData is loaded. This allows us to resolve serialized EntityUid instances correctly.
                AllocEntities();

                // Actually instance components and run ExposeData on them.
                FinishEntitiesLoad();

                // Clear the net tick numbers so that components from prototypes (not modified by map)
                // aren't sent over the wire initially.
                ResetNetTicks();

                // Grid entities were NOT created inside ReadGridSection().
                // We have to fix the created grids up with the grid entities deserialized from the map.
                FixMapEntities();

                // We have to attach grids to the target map here.
                // If we don't, initialization & startup can fail for some entities.
                AttachMapEntities();

                // Run Initialize on all components.
                FinishEntitiesInitialization();

                // Run Startup on all components.
                FinishEntitiesStartup();
            }

            private void VerifyEntitiesExist()
            {
                var fail = false;
                var entities = RootNode.GetNode<YamlSequenceNode>("entities");
                var reportedError = new HashSet<string>();
                foreach (var entityDef in entities.Cast<YamlMappingNode>())
                {
                    if (entityDef.TryGetNode("type", out var typeNode))
                    {
                        var type = typeNode.AsString();
                        if (!_prototypeManager.HasIndex<EntityPrototype>(type) && !reportedError.Contains(type))
                        {
                            Logger.Error("Missing prototype for map: {0}", type);
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
                foreach (var (entity, data) in _entitiesToDeserialize)
                {
                    if (!data.TryGetNode("components", out YamlSequenceNode? componentList))
                    {
                        continue;
                    }

                    if (entity.Prototype == null)
                    {
                        continue;
                    }

                    foreach (var component in _componentManager.GetNetComponents(entity.Uid))
                    {
                        var castComp = (Component) component;

                        if (componentList.Any(p => p["type"].AsString() == component.Name))
                        {
                            if (entity.Prototype.Components.ContainsKey(component.Name))
                            {
                                // This component is modified by the map so we have to send state.
                                // Though it's still in the prototype itself so creation doesn't need to be sent.
                                castComp.ClearCreationTick();
                            }
                            else
                            {
                                // New component that the prototype normally does not have, need to sync full data.
                                continue;
                            }
                        }

                        // This component is not modified by the map file,
                        // so the client will have the same data after instantiating it from prototype ID.
                        castComp.ClearTicks();
                    }
                }
            }

            private void AttachMapEntities()
            {
                var mapEntity = _mapManager.GetMapEntity(TargetMap);

                foreach (var grid in Grids)
                {
                    var entity = _serverEntityManager.GetEntity(grid.GridEntityId);
                    entity.Transform.AttachParent(mapEntity);
                }
            }

            private void FixMapEntities()
            {
                foreach (var entity in Entities)
                {
                    if (entity.TryGetComponent(out IMapGridComponent? grid))
                    {
                        var castGrid = (MapGrid) grid.Grid;
                        castGrid.GridEntityId = entity.Uid;
                    }
                }
            }

            private void ReadMetaSection()
            {
                var meta = RootNode.GetNode<YamlMappingNode>("meta");
                var ver = meta.GetNode("format").AsInt();
                if (ver != MapFormatVersion)
                {
                    throw new InvalidDataException("Cannot handle this map file version.");
                }

                if (meta.TryGetNode("postmapinit", out var mapInitNode))
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

                var tileMap = RootNode.GetNode<YamlMappingNode>("tilemap");
                foreach (var (key, value) in tileMap)
                {
                    var tileId = (ushort) key.AsInt();
                    var tileDefName = value.AsString();
                    _tileMap.Add(tileId, tileDefName);
                }
            }

            private void ReadGridSection()
            {
                var grids = RootNode.GetNode<YamlSequenceNode>("grids");

                foreach (var grid in grids)
                {
                    var newId = new GridId?();
                    YamlGridSerializer.DeserializeGrid(
                        _mapManager, TargetMap, ref newId,
                        (YamlMappingNode) grid["settings"],
                        (YamlSequenceNode) grid["chunks"],
                        _tileMap!,
                        _tileDefinitionManager
                    );

                    if (newId != null)
                    {
                        Grids.Add(_mapManager.GetGrid(newId.Value));
                    }
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
                        _pauseManager.AddUninitializedMap(TargetMap);
                    }
                }
            }

            private void AllocEntities()
            {
                var entities = RootNode.GetNode<YamlSequenceNode>("entities");
                foreach (var entityDef in entities.Cast<YamlMappingNode>())
                {
                    string? type = null;
                    if (entityDef.TryGetNode("type", out var typeNode))
                    {
                        type = typeNode.AsString();
                    }

                    var uid = Entities.Count;
                    if (entityDef.TryGetNode("uid", out var uidNode))
                    {
                        uid = uidNode.AsInt();
                    }

                    var entity = _serverEntityManager.AllocEntity(type);
                    Entities.Add(entity);
                    UidEntityMap.Add(uid, entity.Uid);
                    _entitiesToDeserialize.Add((entity, entityDef));

                    if (_loadOptions!.StoreMapUids)
                    {
                        var comp = entity.AddComponent<MapSaveIdComponent>();
                        comp.Uid = uid;
                    }
                }
            }

            private void FinishEntitiesLoad()
            {
                foreach (var (entity, data) in _entitiesToDeserialize)
                {
                    CurrentReadingEntityComponents = new Dictionary<string, YamlMappingNode>();
                    if (data.TryGetNode("components", out YamlSequenceNode? componentList))
                    {
                        foreach (var compData in componentList)
                        {
                            var copy = new YamlMappingNode(((YamlMappingNode)compData).AsEnumerable());
                            copy.Children.Remove(new YamlScalarNode("type"));
                            //TODO Paul: maybe replace mapping with dict
                            CurrentReadingEntityComponents[compData["type"].AsString()] = copy;
                        }
                    }

                    _serverEntityManager.FinishEntityLoad(entity, this);
                }
            }

            private void FinishEntitiesInitialization()
            {
                foreach (var entity in Entities)
                {
                    _serverEntityManager.FinishEntityInitialization(entity);
                }
            }

            private void FinishEntitiesStartup()
            {
                foreach (var entity in Entities)
                {
                    _serverEntityManager.UpdateEntityTree(entity);
                }

                foreach (var entity in Entities)
                {
                    _serverEntityManager.FinishEntityStartup(entity);
                }

                foreach (var entity in Entities)
                {
                    _serverEntityManager.UpdateEntityTree(entity);
                }
            }

            // Serialization
            public void RegisterGrid(IMapGrid grid)
            {
                if (GridIDMap.ContainsKey(grid.Index))
                {
                    throw new InvalidOperationException();
                }

                Grids.Add(grid);
                GridIDMap.Add(grid.Index, GridIDMap.Count);
            }

            public YamlNode Serialize()
            {
                WriteMetaSection();
                WriteTileMapSection();
                WriteGridSection();

                PopulateEntityList();
                WriteEntitySection();

                return RootNode;
            }

            private void WriteMetaSection()
            {
                var meta = new YamlMappingNode();
                RootNode.Add("meta", meta);
                meta.Add("format", MapFormatVersion.ToString(CultureInfo.InvariantCulture));
                // TODO: Make these values configurable.
                meta.Add("name", "DemoStation");
                meta.Add("author", "Space-Wizards");

                var isPostInit = false;
                foreach (var grid in Grids)
                {
                    if (_pauseManager.IsMapInitialized(grid.ParentMapId))
                    {
                        isPostInit = true;
                        break;
                    }
                }

                meta.Add("postmapinit", isPostInit ? "true" : "false");
            }

            private void WriteTileMapSection()
            {
                var tileMap = new YamlMappingNode();
                RootNode.Add("tilemap", tileMap);
                foreach (var tileDefinition in _tileDefinitionManager)
                {
                    tileMap.Add(tileDefinition.TileId.ToString(CultureInfo.InvariantCulture), tileDefinition.Name);
                }
            }

            private void WriteGridSection()
            {
                var grids = new YamlSequenceNode();
                RootNode.Add("grids", grids);

                foreach (var grid in Grids)
                {
                    var entry = YamlGridSerializer.SerializeGrid(grid);
                    grids.Add(entry);
                }
            }

            private void PopulateEntityList()
            {
                var withUid = new List<MapSaveIdComponent>();
                var withoutUid = new List<IEntity>();
                var takenIds = new HashSet<int>();

                foreach (var entity in _serverEntityManager.GetEntities())
                {
                    if (IsMapSavable(entity))
                    {
                        Entities.Add(entity);
                        if (entity.TryGetComponent(out MapSaveIdComponent? mapSaveId))
                        {
                            withUid.Add(mapSaveId);
                        }
                        else
                        {
                            withoutUid.Add(entity);
                        }
                    }
                }

                // Go over entities with a MapSaveIdComponent and assign those.

                foreach (var mapIdComp in withUid)
                {
                    var uid = mapIdComp.Uid;
                    if (takenIds.Contains(uid))
                    {
                        // Duplicate ID. Just pretend it doesn't have an ID and use the without path.
                        withoutUid.Add(mapIdComp.Owner);
                    }
                    else
                    {
                        EntityUidMap.Add(mapIdComp.Owner.Uid, uid);
                        takenIds.Add(uid);
                    }
                }

                var uidCounter = 0;
                foreach (var entity in withoutUid)
                {
                    while (takenIds.Contains(uidCounter))
                    {
                        // Find next available UID.
                        uidCounter += 1;
                    }

                    EntityUidMap.Add(entity.Uid, uidCounter);
                    takenIds.Add(uidCounter);
                }
            }

            private void WriteEntitySection()
            {
                var serv3Mgr = IoCManager.Resolve<ISerializationManager>();
                var entities = new YamlSequenceNode();
                RootNode.Add("entities", entities);

                foreach (var entity in Entities.OrderBy(e => EntityUidMap[e.Uid]))
                {
                    CurrentWritingEntity = entity;
                    var mapping = new YamlMappingNode
                    {
                        {"uid", EntityUidMap[entity.Uid].ToString(CultureInfo.InvariantCulture)}
                    };

                    if (entity.Prototype != null)
                    {
                        mapping.Add("type", entity.Prototype.ID);
                    }

                    var components = new YamlSequenceNode();
                    // See engine#636 for why the Distinct() call.
                    foreach (var component in entity.GetAllComponents())
                    {
                        if (component is MapSaveIdComponent)
                            continue;

                        CurrentWritingComponent = component.Name;
                        var compMapping = (MappingDataNode)serv3Mgr.WriteValue(component.GetType(), component, context: this);

                        // Don't need to write it if nothing was written!
                        if (compMapping.Children.Count != 0)
                        {
                            compMapping.AddNode("type", new ValueDataNode(component.Name));
                            // Something actually got written!
                            components.Add(compMapping.ToYamlNode());
                        }
                    }

                    if (components.Children.Count != 0)
                    {
                        mapping.Add("components", components);
                    }

                    entities.Add(mapping);
                }
            }

            // Create custom object serializers that will correctly allow data to be overriden by the map file.
            IComponent IEntityLoadContext.GetComponentData(string componentName,
                IComponent? protoData)
            {
                if (CurrentReadingEntityComponents == null)
                {
                    throw new InvalidOperationException();
                }

                var serv3Mgr = IoCManager.Resolve<ISerializationManager>();
                var factory = IoCManager.Resolve<IComponentFactory>();

                IComponent data = protoData != null ? (IComponent)serv3Mgr.CreateCopy(protoData)! : (IComponent)Activator.CreateInstance(factory.GetRegistration(componentName).Type)!;

                if (CurrentReadingEntityComponents.TryGetValue(componentName, out var mapping))
                {
                    data = serv3Mgr.ReadValue<IComponent>(factory.GetRegistration(componentName).Type,
                        mapping.ToDataNode(), this);
                }

                return data;
            }

            public IEnumerable<string> GetExtraComponentTypes()
            {
                return CurrentReadingEntityComponents!.Keys;
            }

            private bool IsMapSavable(IEntity entity)
            {
                if (entity.Prototype?.MapSavable == false || !GridIDMap.ContainsKey(entity.Transform.GridID))
                {
                    return false;
                }

                // Don't serialize things parented to un savable things.
                // For example clothes inside a person.
                var current = entity.Transform;
                while (current.Parent != null)
                {
                    if (current.Parent.Owner.Prototype?.MapSavable == false)
                    {
                        return false;
                    }

                    current = current.Parent;
                }

                return true;
            }

            public GridId Read(ValueDataNode node, ISerializationContext? context = null)
            {
                if(node.Value == "null") return GridId.Invalid;

                var val = int.Parse(node.Value);
                if (val >= Grids.Count)
                {
                    Logger.ErrorS("map", "Error in map file: found local grid ID '{0}' which does not exist.", val);
                }
                else
                {
                    return Grids[val].Index;
                }
                return GridId.Invalid;
            }

            public DataNode Write(IEntity value, bool alwaysWrite = false,
                ISerializationContext? context = null)
            {
                if (!EntityUidMap.TryGetValue(value.Uid, out var entityMapped))
                {
                    Logger.WarningS("map", "Cannot write entity UID '{0}'.", value.Uid);
                    return new ValueDataNode("");
                }
                else
                {
                    return new ValueDataNode(entityMapped.ToString(CultureInfo.InvariantCulture));
                }
            }

            public DataNode Write(EntityUid value, bool alwaysWrite = false,
                ISerializationContext? context = null)
            {
                if (!EntityUidMap.TryGetValue(value, out var entityUidMapped))
                {
                    // Terrible hack to mute this warning on the grids themselves when serializing blueprints.
                    if (!IsBlueprintMode || !CurrentWritingEntity!.HasComponent<MapGridComponent>() ||
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

            public DataNode Write(GridId value, bool alwaysWrite = false,
                ISerializationContext? context = null)
            {
                if (!GridIDMap.TryGetValue(value, out var gridMapped))
                {
                    Logger.WarningS("map", "Cannot write grid ID '{0}', falling back to nullspace.", gridMapped);
                    return new ValueDataNode("");
                }
                else
                {
                    return new ValueDataNode(gridMapped.ToString(CultureInfo.InvariantCulture));
                }
            }

            EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
            {
                if (node.Value == "null")
                {
                    return EntityUid.Invalid;
                }

                var val = int.Parse(node.Value);
                if (val >= Entities.Count)
                {
                    Logger.ErrorS("map", "Error in map file: found local entity UID '{0}' which does not exist.", val);
                }
                else
                {
                    return UidEntityMap[val];
                }
                return EntityUid.Invalid;
            }

            IEntity ITypeReader<IEntity, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
            {
                var val = int.Parse(node.Value);
                if (val >= Entities.Count)
                {
                    Logger.ErrorS("map", "Error in map file: found local entity UID '{0}' which does not exist.", val);
                    return null!;
                }
                else
                {
                    return Entities[val];
                }
            }
        }

        /// <summary>
        ///     Does basic pre-deserialization checks on map file load.
        ///     For example, let's not try to use maps with multiple grids as blueprints, shall we?
        /// </summary>
        private class MapData
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
