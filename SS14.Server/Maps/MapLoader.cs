using System;
using System.IO;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Maps;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Prototypes;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Utility;

namespace SS14.Server.Maps
{
    /// <summary>
    ///     Saves and loads maps to the disk.
    /// </summary>
    public class MapLoader : IMapLoader
    {
        private const int MapFormatVersion = 1;

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

            var root = SaveBpNode(grid);

            var document = new YamlDocument(root);

            var rootPath = _resMan.ConfigDirectory;
            var path = Path.Combine(rootPath.ToString(), "./", yamlPath);
            var fullPath = Path.GetFullPath(path);

            var dir = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dir ?? throw new InvalidOperationException("Full YamlPath was null."));

            using (var writer = new StreamWriter(fullPath))
            {
                var stream = new YamlStream();

                stream.Add(document);
                stream.Save(writer);
            }
        }

        /// <inheritdoc />
        public IMapGrid LoadBlueprint(IMap map, string path, GridId? newId = null)
        {
            var rootPath = _resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, "./", path);
            var fullPath = Path.GetFullPath(comb);

            TextReader reader;

            // try user
            if (!File.Exists(fullPath))
            {
                Logger.InfoS("map", $"No user blueprint path: {fullPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(ResourcePath.Root / path, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No blueprint found: {path}");
                    return null;
                }
            }
            else
            {
                reader = new StreamReader(fullPath);
            }

            using (reader)
            {
                Logger.InfoS("map", $"Loading Grid: {path}");

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
            var root = new YamlMappingNode();

            root.Add("format", MapFormatVersion.ToString());
            root.Add("name", "DemoStation");
            root.Add("author", "Space-Wizards");

            // save grids
            var gridMap = new YamlMappingNode();
            root.Add("grids", gridMap);

            foreach (var grid in map.GetAllGrids())
            {
                var gridBpNode = SaveBpNode(grid);
                gridMap.Add(grid.Index.ToString(), gridBpNode);
            }

            var document = new YamlDocument(root);

            var rootPath = _resMan.ConfigDirectory;
            var path = Path.Combine(rootPath, "./", yamlPath);
            var fullPath = Path.GetFullPath(path);

            var dir = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dir ?? throw new InvalidOperationException("Full YamlPath was null."));

            using (var writer = new StreamWriter(fullPath))
            {
                var stream = new YamlStream();

                stream.Add(document);
                stream.Save(writer);
            }
        }

        /// <inheritdoc />
        public void LoadMap(MapId mapId, string path)
        {
            // FIXME: The handling of grid IDs in here is absolutely 100% turbofucked ATM.
            // This function absolutely will not work.
            // It's CURRENTLY still working using the old map ID -> grid ID which absolutely DOES NOT WORK ANYMORE.
            // BP loading works because grid IDs are manually hard set.
            var rootPath = _resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, "./", path);
            var fullPath = Path.GetFullPath(comb);

            TextReader reader;

            // try user
            if (!File.Exists(fullPath))
            {
                Logger.InfoS("map", $"No user blueprint found: {fullPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(path, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No blueprint found: {path}");
                    return;
                }
            }
            else
            {
                reader = new StreamReader(fullPath);
            }

            using (reader)
            {
                Logger.InfoS("map", $"Loading Map: {path}");

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

        private YamlSequenceNode SaveBpNode(IMapGrid grid)
        {
            var root = new YamlSequenceNode();

            var node = YamlGridSerializer.SerializeGrid(grid);
            root.Add(node);

            var ents = new YamlEntitySerializer();
            _entityMan.SaveGridEntities(ents, grid.Index);
            root.Add(ents.GetRootNode());
            return root;
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

                    _protoMan.LoadData(entity, yamlEnt);

                    // overwrite local position in the BP to the new map/grid ID
                    var transform = entity.GetComponent<IServerTransformComponent>();
                    transform.LocalPosition = new GridLocalCoordinates(transform.LocalPosition.Position, gridId.Value);
                }
                catch (Exception e)
                {
                    Logger.ErrorS("map", $"Error creating entity \"{protoName}\": {e.Message}");
                }
            }
        }
    }
}
