using System;
using System.IO;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Maps;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

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
        public void SaveBlueprint(IMap map, GridId gridId, string yamlPath)
        {
            var grid = map.GetGrid(gridId);

            var root = SaveBpNode(grid);

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
        public void LoadBlueprint(IMap map, GridId newId, string path)
        {
            var rootPath = _resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, "./", path);
            var fullPath = Path.GetFullPath(comb);

            TextReader reader;

            // try user
            if (!File.Exists(fullPath))
            {
                Logger.Info($"[MAP] No user blueprint path: {fullPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(path, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.Error($"[MAP] No blueprint found: {path}");
                    return;
                }
            }
            else
            {
                reader = new StreamReader(fullPath);
            }

            using (reader)
            {
                Logger.Info($"[MAP] Loading Grid: {path}");

                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    var root = (YamlSequenceNode)document.RootNode;
                    LoadBpNode(map, newId, root);
                }
            }
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
            var rootPath = _resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, "./", path);
            var fullPath = Path.GetFullPath(comb);

            TextReader reader;

            // try user
            if (!File.Exists(fullPath))
            {
                Logger.Info($"[MAP] No user blueprint found: {fullPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(path, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.Error($"[MAP] No blueprint found: {path}");
                    return;
                }
            }
            else
            {
                reader = new StreamReader(fullPath);
            }

            using (reader)
            {
                Logger.Info($"[MAP] Loading Map: {path}");

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

        private void LoadBpNode(IMap map, GridId newId, YamlSequenceNode root)
        {
            foreach (var yamlNode in root.Children)
            {
                var mapNode = (YamlMappingNode)yamlNode;

                if (mapNode.Children.TryGetValue("grid", out var gridNode))
                {
                    // default grid always exists, and cannot be modified, no point loading it
                    if (newId == GridId.DefaultGrid)
                        continue;

                    var gridMap = (YamlMappingNode)gridNode;
                    LoadGridNode(_mapManager, map, newId, gridMap);
                }
                else if (mapNode.Children.TryGetValue("entities", out var entNode))
                {
                    LoadEntNode(map, newId, (YamlSequenceNode)entNode);
                }
            }
        }

        private static void LoadGridNode(IMapManager mapMan, IMap map, GridId newId, YamlMappingNode gridNode)
        {
            var info = (YamlMappingNode)gridNode["settings"];
            var chunk = (YamlSequenceNode)gridNode["chunks"];

            YamlGridSerializer.DeserializeGrid(mapMan, map, newId, info, chunk);
        }

        private void LoadEntNode(IMap map, GridId gridId, YamlSequenceNode entNode)
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
                    transform.LocalPosition = new LocalCoordinates(transform.LocalPosition.Position, gridId, map.Index);
                }
                catch (Exception e)
                {
                    Logger.Error($"[MAP] Error creating entity \"{protoName}\": {e.Message}");
                }
            }
        }
    }
}
