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
            var root = context.DoSerialize();
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
        public IMapGrid LoadBlueprint(IMap map, string path, GridId? newId = null)
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

                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    var root = (YamlSequenceNode)document.RootNode;
                    return LoadBpNode(map, newId, root);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public void SaveMap(IMap map, string yamlPath)
        {
            var context = new MapContext();
            foreach (var grid in map.GetAllGrids())
            {
                context.RegisterGrid(grid);
            }

            var document = new YamlDocument(context.DoSerialize());

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

                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    var root = (YamlMappingNode)document.RootNode;
                    var map = _mapManager.CreateMap(mapId);
                    LoadMapNode(map, root);
                }
            }
        }

        private void LoadMapNode(IMap map, YamlMappingNode rootNode)
        {
            var gridSeq = (YamlMappingNode)rootNode["grids"];

            foreach (var kvGrid in gridSeq)
            {
                var gridId = int.Parse(kvGrid.Key.ToString());
                var gridNode = (YamlSequenceNode)kvGrid.Value;

                LoadBpNode(map, new GridId(gridId), gridNode);
            }
        }

        private IMapGrid LoadBpNode(IMap map, GridId? newId, YamlSequenceNode root)
        {
            foreach (var yamlNode in root.Children)
            {
                var mapNode = (YamlMappingNode)yamlNode;

                if (mapNode.Children.TryGetValue("grid", out var gridNode))
                {
                    var gridMap = (YamlMappingNode)gridNode;
                    // This ref shit is so that the entities are loaded with the grid created by this load.
                    LoadGridNode(_mapManager, map, ref newId, gridMap);
                }
                else if (mapNode.Children.TryGetValue("entities", out var entNode))
                {
                    LoadEntNode(map, newId, (YamlSequenceNode)entNode);
                }
            }

            return IoCManager.Resolve<IMapManager>().GetGrid(newId.Value);
        }

        private static void LoadGridNode(IMapManager mapMan, IMap map, ref GridId? newId, YamlMappingNode gridNode)
        {
            var info = (YamlMappingNode)gridNode["settings"];
            var chunk = (YamlSequenceNode)gridNode["chunks"];

            YamlGridSerializer.DeserializeGrid(mapMan, map, ref newId, info, chunk);
        }

        private void LoadEntNode(IMap map, GridId? gridId, YamlSequenceNode entNode)
        {
            foreach (var yamlNode in entNode.Children)
            {
                var yamlEnt = (YamlMappingNode)yamlNode;

                var protoName = yamlEnt["id"].ToString();

                try
                {
                    var entity = _entityMan.SpawnEntity(protoName);

                    // overwrite local position in the BP to the new map/grid ID
                    var transform = entity.GetComponent<IServerTransformComponent>();
                    transform.LocalPosition = new GridLocalCoordinates(transform.LocalPosition.Position, gridId.Value);
                }
                catch (Exception e)
                {
                    Logger.ErrorS("map", $"Error creating entity \"{protoName}\": {e}");
                }
            }
        }

        /// <summary>
        ///     Handles the primary bulk of state during the map serialization process.
        /// </summary>
        private class MapContext : IYamlObjectSerializerContext
        {
            readonly Dictionary<GridId, int> GridIDMap = new Dictionary<GridId, int>();
            readonly List<IMapGrid> Grids = new List<IMapGrid>();

            readonly Dictionary<EntityUid, int> EntityUidMap = new Dictionary<EntityUid, int>();
            readonly List<IEntity> Entities = new List<IEntity>();

            YamlMappingNode RootNode;

            public void RegisterGrid(IMapGrid grid)
            {
                if (GridIDMap.ContainsKey(grid.Index))
                {
                    throw new InvalidOperationException();
                }

                Grids.Add(grid);
                GridIDMap.Add(grid.Index, GridIDMap.Count);
            }

            public YamlNode DoSerialize()
            {
                RootNode = new YamlMappingNode();

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

            private void PopulateEntityList()
            {
                var entMgr = IoCManager.Resolve<IEntityManager>();
                foreach (var entity in entMgr.GetEntities())
                {
                    if (entity.TryGetComponent(out IServerTransformComponent transform) && GridIDMap.ContainsKey(transform.GridID))
                    {
                        // Welcome aboard!
                        EntityUidMap.Add(entity.Uid, EntityUidMap.Count);
                        Entities.Add(entity);
                    }
                }
            }

            private void WriteEntitySection()
            {
                var entities = new YamlSequenceNode();
                RootNode.Add("entities", entities);

                foreach (var entity in Entities)
                {
                    var mapping = new YamlMappingNode();
                    var serializer = new YamlObjectSerializer(mapping, reading: false, context: this);
                    entity.ExposeData(serializer);

                    mapping.Add("type", entity.Prototype.ID);

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
