using System;
using System.IO;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Maps;
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
        [Dependency]
        private IResourceManager _resMan;

        [Dependency]
        private IServerEntityManager _entityMan;

        [Dependency]
        private IPrototypeManager _protoMan;

        /// <inheritdoc />
        public void LoadGrid(IMap map, string path)
        {
            var rootPath = _resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, path);
            var fullPath = Path.GetFullPath(comb);

            if (!File.Exists(fullPath))
            {
                Logger.Error($"[MAP] Cannot load map path: {fullPath}");
                return;
            }

            Logger.Info($"[MAP] Loading Grid: {path}");

            using (var reader = new StreamReader(fullPath))
            {
                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    LoadGridDocument(map, document);
                }
            }
        }

        /// <inheritdoc />
        public void SaveGrid(IMap map, GridId gridId, string yamlPath)
        {
            var grid = map.GetGrid(gridId);

            var node = YamlGridSerializer.SerializeGrid(grid);
            var document = new YamlDocument(node);

            var rootPath = _resMan.ConfigDirectory;
            var path = Path.Combine(rootPath, yamlPath);
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
        public void LoadEntities(IMap map, string path)
        {
            var rootPath = _resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, path);
            var fullPath = Path.GetFullPath(comb);

            if (!File.Exists(fullPath))
            {
                Logger.Error($"[MAP] Cannot load entity path: {fullPath}");
                return;
            }

            Logger.Info($"[MAP] Loading Entities: {path}");

            using (var reader = new StreamReader(fullPath))
            {
                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    LoadEntityDocument(document);
                }
            }
        }

        private static void LoadGridDocument(IMap map, YamlDocument document)
        {
            var root = (YamlSequenceNode) document.RootNode;

            foreach (var yamlNode in root.Children)
            {
                var gridNode = (YamlMappingNode) yamlNode;
                var info = (YamlMappingNode) gridNode[new YamlScalarNode("settings")];
                var chunk = (YamlSequenceNode) gridNode[new YamlScalarNode("chunks")];

                YamlGridSerializer.DeserializeGrid(map, new GridId(1), info, chunk);
            }
        }

        private void LoadEntityDocument(YamlDocument doc)
        {
            // first node is always sequence
            var root = (YamlSequenceNode) doc.RootNode;

            // sequence always contains mappings
            foreach (var yamlNode in root.Children)
            {
                var yamlEnt = (YamlMappingNode) yamlNode;
                LoadEntityYaml(yamlEnt);
            }
        }

        private void LoadEntityYaml(YamlMappingNode node)
        {
            var protoName = node[new YamlScalarNode("id")].ToString();

            var entity = _entityMan.SpawnEntity(protoName);

            _protoMan.LoadData(entity, node);
        }
    }
}
