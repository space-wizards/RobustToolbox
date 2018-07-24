using System;
using System.Collections.Generic;
using System.IO;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Maps;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Prototypes;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Utility;
using SS14.Shared.Serialization;
using SS14.Shared.GameObjects;
using System.Globalization;
using SS14.Shared.Interfaces.GameObjects;
using System.Linq;

namespace SS14.Server.Maps
{
    /// <summary>
    ///     Saves and loads maps to the disk.
    /// </summary>
    public class MapLoader : IMapLoader
    {
        private const int MapFormatVersion = 2;

        [Dependency]
        private IResourceManager _resMan;

        [Dependency]
        private IServerEntityManager _entityMan;

        [Dependency]
        private IPrototypeManager _protoMan;

        [Dependency]
        private readonly IMapManager _mapManager;

        /// <inheritdoc />
        public void SaveBlueprint(GridId gridId, string yamlPath)
        {
            var grid = _mapManager.GetGrid(gridId);

            var context = new MapContext();
            context.RegisterGrid(grid);
            var root = context.Serialize();
            var document = new YamlDocument(root);

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using (var file = _resMan.UserData.Open(resPath, FileMode.OpenOrCreate))
            {
                using (var writer = new StreamWriter(file))
                {
                    var stream = new YamlStream();

                    stream.Add(document);
                    stream.Save(writer);
                }
            }
        }

        /// <inheritdoc />
        public IMapGrid LoadBlueprint(IMap map, string path)
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
                var file = _resMan.UserData.Open(resPath, FileMode.Open);
                reader = new StreamReader(file);
            }

            using (reader)
            {
                Logger.InfoS("map", $"Loading Grid: {resPath}");

                var data = new MapData(reader);

                if (data.GridCount != 1)
                {
                    throw new InvalidDataException("Cannot instance map with multiple grids as blueprint.");
                }

                var context = new MapContext((YamlMappingNode)data.RootNode, map);
                context.Deserialize();
                return context.Grids[0];
            }
        }

        /// <inheritdoc />
        public void SaveMap(IMap map, string yamlPath)
        {
            var context = new MapContext();
            foreach (var grid in map.GetAllGrids())
            {
                context.RegisterGrid(grid);
            }

            var document = new YamlDocument(context.Serialize());

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using (var file = _resMan.UserData.Open(resPath, FileMode.OpenOrCreate))
            {
                using (var writer = new StreamWriter(file))
                {
                    var stream = new YamlStream();

                    stream.Add(document);
                    stream.Save(writer);
                }
            }
        }

        /// <inheritdoc />
        public void LoadMap(MapId mapId, string path)
        {
            // FIXME: The handling of grid IDs in here is absolutely 100% turbofucked ATM.
            // This function absolutely will not work.
            // It's CURRENTLY still working using the old map ID -> grid ID which absolutely DOES NOT WORK ANYMORE.
            // BP loading works because grid IDs are manually hard set.

            TextReader reader;
            var resPath = new ResourcePath(path).ToRootedPath();

            // try user
            if (!_resMan.UserData.Exists(resPath))
            {
                Logger.InfoS("map", $"No user blueprint found: {resPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(resPath, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No blueprint found: {resPath}");
                    return;
                }
            }
            else
            {
                var file = _resMan.UserData.Open(resPath, FileMode.Open);
                reader = new StreamReader(file);
            }

            using (reader)
            {
                Logger.InfoS("map", $"Loading Map: {resPath}");

                var data = new MapData(reader);

                if (data.GridCount != 1)
                {
                    throw new InvalidDataException("Cannot instance map with multiple grids as blueprint.");
                }

                var context = new MapContext((YamlMappingNode)data.RootNode, _mapManager.GetMap(mapId));
                context.Deserialize();
            }
        }

        /// <summary>
        ///     Handles the primary bulk of state during the map serialization process.
        /// </summary>
        private class MapContext : IYamlObjectSerializerContext, IEntityFinishContext
        {
            public readonly Dictionary<GridId, int> GridIDMap = new Dictionary<GridId, int>();
            public readonly List<IMapGrid> Grids = new List<IMapGrid>();

            public readonly Dictionary<EntityUid, int> EntityUidMap = new Dictionary<EntityUid, int>();
            public readonly List<IEntity> Entities = new List<IEntity>();

            readonly YamlMappingNode RootNode;
            readonly IMap TargetMap;

            YamlMappingNode CurrentReadingEntity;
            Dictionary<string, YamlMappingNode> CurrentReadingEntityComponents;

            public MapContext()
            {
                RootNode = new YamlMappingNode();
            }

            public MapContext(YamlMappingNode node, IMap targetMap)
            {
                RootNode = node;
                TargetMap = targetMap;
            }

            // Deserialization
            public void Deserialize()
            {
                ReadMetaSection();
                ReadGridSection();

                // Entities are allocated in a separate step so entity UID cross references can be resolved.
                AllocEntities();
                FinishEntities();
            }

            void ReadMetaSection()
            {
                var meta = RootNode.GetNode<YamlMappingNode>("meta");
                var ver = meta.GetNode("format").AsInt();
                if (ver != MapFormatVersion)
                {
                    throw new InvalidDataException("Cannot handle this map file version.");
                }
            }

            void ReadGridSection()
            {
                var grids = RootNode.GetNode<YamlSequenceNode>("grids");
                var mapMan = IoCManager.Resolve<IMapManager>();

                foreach (var grid in grids)
                {
                    var newId = new GridId?();
                    YamlGridSerializer.DeserializeGrid(
                        mapMan, TargetMap, ref newId,
                        (YamlMappingNode)grid["settings"],
                        (YamlSequenceNode)grid["chunks"]
                    );

                    Grids.Add(mapMan.GetGrid(newId.Value));
                }
            }

            void AllocEntities()
            {
                var entities = RootNode.GetNode<YamlSequenceNode>("entities");
                var entityMan = IoCManager.Resolve<IServerEntityManagerInternal>();

                foreach (var entityDef in entities.Cast<YamlMappingNode>())
                {
                    var type = entityDef.GetNode("id").AsString();
                    var entity = entityMan.AllocEntity(type);
                    Entities.Add(entity);
                    if (entityDef.TryGetNode("name", out var nameNode))
                    {
                        entity.Name = nameNode.AsString();
                    }
                }
            }

            void FinishEntities()
            {
                var entities = RootNode.GetNode<YamlSequenceNode>("entities");
                var entityMan = IoCManager.Resolve<IServerEntityManagerInternal>();

                foreach (var (entity, data) in Entities.Zip(entities, (a, b) => (a, (YamlMappingNode)b)))
                {
                    CurrentReadingEntity = (YamlMappingNode)data;
                    CurrentReadingEntityComponents = new Dictionary<string, YamlMappingNode>();
                    if (data.TryGetNode("components", out YamlSequenceNode componentList))
                    {
                        foreach (var compData in componentList)
                        {
                            CurrentReadingEntityComponents[compData["type"].AsString()] = (YamlMappingNode)compData;
                        }
                    }
                    entityMan.FinishEntity(entity, this);
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
                WriteGridSection();

                PopulateEntityList();
                WriteEntitySection();

                return RootNode;
            }

            void WriteMetaSection()
            {
                var meta = new YamlMappingNode();
                RootNode.Add("meta", meta);
                meta.Add("format", MapFormatVersion.ToString(CultureInfo.InvariantCulture));
                // TODO: Make these values configurable.
                meta.Add("name", "DemoStation");
                meta.Add("author", "Space-Wizards");
            }

            void WriteGridSection()
            {
                var grids = new YamlSequenceNode();
                RootNode.Add("grids", grids);

                foreach (var grid in Grids)
                {
                    var entry = YamlGridSerializer.SerializeGrid(grid);
                    grids.Add(entry);
                }
            }

            void PopulateEntityList()
            {
                var entMgr = IoCManager.Resolve<IEntityManager>();
                foreach (var entity in entMgr.GetEntities())
                {
                    if (entity.TryGetComponent(out IServerTransformComponent transform) && GridIDMap.ContainsKey(transform.GridID))
                    {
                        EntityUidMap.Add(entity.Uid, EntityUidMap.Count);
                        Entities.Add(entity);
                    }
                }
            }

            void WriteEntitySection()
            {
                var entities = new YamlSequenceNode();
                RootNode.Add("entities", entities);

                foreach (var entity in Entities)
                {
                    var mapping = new YamlMappingNode();
                    mapping.Add("type", entity.Prototype.ID);
                    if (entity.Name != entity.Prototype.Name)
                    {
                        // TODO: This shouldn't be hardcoded.
                        mapping.Add("name", entity.Prototype.Name);
                    }

                    var components = new YamlSequenceNode();
                    foreach (var component in entity.GetAllComponents())
                    {
                        var compMapping = new YamlMappingNode();
                        var compSerializer = new YamlObjectSerializer(mapping, reading: false, context: this);

                        component.ExposeData(compSerializer);

                        // Don't need to write it if nothing was written!
                        if (compMapping.Children.Count != 0)
                        {
                            // Something actually got written!
                            compMapping.Add("type", component.Name);
                            components.Add(compMapping);
                        }
                    }

                    if (components.Children.Count != 0)
                    {
                        mapping.Add("components", components);
                    }
                }
            }

            public bool TryNodeToType(YamlNode node, Type type, out object obj)
            {
                obj = null;
                return false;
            }

            public bool TryTypeToNode(object obj, out YamlNode node)
            {
                node = null;
                return false;
            }

            ObjectSerializer IEntityFinishContext.GetComponentSerializer(string componentName, YamlMappingNode protoData)
            {
                if (CurrentReadingEntityComponents == null)
                {
                    throw new InvalidOperationException();
                }
                if (CurrentReadingEntityComponents.TryGetValue(componentName, out var mapping))
                {
                    return new YamlObjectSerializer(mapping, reading: true, context: this, backups: new List<YamlMappingNode> { protoData });
                }

                return new YamlObjectSerializer(protoData, reading: true, context: this);
            }
        }

        /// <summary>
        ///     Does basic pre-deserialization checks on map file load.
        ///     For example, let's not try to use maps with multiple grids as blueprints, shall we?
        /// </summary>
        private class MapData
        {
            public YamlNode RootNode { get; }
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

                RootNode = stream.Documents[0].RootNode;
                GridCount = ((YamlSequenceNode)RootNode["grids"]).Children.Count;
            }
        }
    }
}
