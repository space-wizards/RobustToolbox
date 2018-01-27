using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Maps;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.Maps
{
    public class MapLoader : IMapLoader
    {
        [Dependency]
        private IResourceManager resourceManager;

        [Dependency]
        private IServerEntityManager entityManager;

        [Dependency]
        private IPrototypeManager protoMan;

        /// <inheritdoc />
        public void LoadGrid(IMap map, string path)
        {
            var resMan = IoCManager.Resolve<IResourceManager>();

            var rootPath = resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, path);
            var fullPath = Path.GetFullPath(comb);

            if (!File.Exists(fullPath))
            {
                Logger.Error($"[MAP] Cannot load map path: {fullPath}");
                return;
            }

            using (var reader = new StreamReader(fullPath))
            {
                var stream = new YamlStream();
                stream.Load(reader);

                foreach (var document in stream.Documents)
                {
                    LoadGridDocument(document);
                }
            }
        }

        private void LoadGridDocument(YamlDocument document)
        {
            var root = (YamlSequenceNode)document.RootNode;
            
            var mapMan = IoCManager.Resolve<IMapManager>();
            var map = mapMan.GetMap(new MapId(1));

            foreach (YamlMappingNode gridNode in root.Children)
            {
                var info = (YamlMappingNode)gridNode[new YamlScalarNode("settings")];
                var chunk = (YamlSequenceNode)gridNode[new YamlScalarNode("chunks")];

                YamlGridSerializer.DeserializeGrid(map, new GridId(1), info, chunk);
            }
        }

        public void SaveGrid(IMap map, GridId gridId, string yamlPath)
        {
            var grid = map.GetGrid(gridId);

            var node = YamlGridSerializer.SerializeGrid(grid);
            var document = new YamlDocument(node);

            var resMan = IoCManager.Resolve<IResourceManager>();
            var rootPath = resMan.ConfigDirectory;
            var path = Path.Combine(rootPath, yamlPath);
            var fullPath = Path.GetFullPath(path);

            var dir = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dir);

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
            var resMan = IoCManager.Resolve<IResourceManager>();

            var rootPath = resMan.ConfigDirectory;
            var comb = Path.Combine(rootPath, path);
            var fullPath = Path.GetFullPath(comb);

            if (!File.Exists(fullPath))
            {
                Logger.Error($"[MAP] Cannot load entity path: {fullPath}");
                return;
            }

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

        private void LoadEntityDocument(YamlDocument doc)
        {
            // first node is always sequence
            var root = (YamlSequenceNode)doc.RootNode;

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

            var entity = entityManager.SpawnEntity(protoName);
            
            protoMan.LoadData(entity, node);
        }

        #region OldCode

        public void EntityLoader(IMap map, string filename)
        {
            if (!resourceManager.TryContentFileRead(filename, out var stream))
            {
                throw new ArgumentException($"Map file does not exist: {filename}");
            }
            var savedEntities = XDocument.Load(stream).Element("SavedEntities");

            foreach (var element in savedEntities.Descendants("SavedEntity"))
            {
                LoadEntity(element, map);
            }
        }

        private void LoadEntity(XElement element, IMap map)
        {
            var X = float.Parse(element.Attribute("X").Value, CultureInfo.InvariantCulture);
            var Y = float.Parse(element.Attribute("Y").Value, CultureInfo.InvariantCulture);

            var dir = Direction.South;
            if (element.Attribute("direction") != null)
            {
                dir = (Direction)Enum.Parse(typeof(Direction), element.Attribute("direction").Value, true);
            }

            string prototype = element.Attribute("template").Value;
            IEntity entity;
            try
            {
                entity = entityManager.ForceSpawnEntityAt(prototype, new LocalCoordinates(X, Y, GridId.DefaultGrid, map.Index));
            }
            catch (UnknownPrototypeException)
            {
                Logger.Error($"Unknown prototype '{prototype}'!");
                return;
            }
            var nameElement = element.Attribute("name");
            if (nameElement != null)
            {
                entity.Name = nameElement.Value;
            }
            entity.GetComponent<IServerTransformComponent>().Rotation = dir.ToAngle();
        }

        public void Save(IMap map, string filename)
        {
            var rootElement = new XElement("SavedEntities");

            foreach (var entity in entityManager.GetEntities(new AllEntityQuery()))
            {
                var transform = entity.GetComponent<IServerTransformComponent>();
                if (transform.MapID != map.Index || !entity.Prototype.MapSavable)
                {
                    continue;
                }

                rootElement.Add(ConvertEntityToXML(entity, transform));
            }

            var saveFile = new XDocument(rootElement);
            saveFile.Save(filename);
        }

        private XElement ConvertEntityToXML(IEntity entity, IServerTransformComponent transform)
        {
            var element = new XElement("SavedEntity");
            element.SetAttributeValue("X", transform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("Y", transform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("template", entity.Prototype.ID);
            if (entity.Name != entity.Prototype.Name)
            {
                element.SetAttributeValue("name", entity.Name);
            }
            var dir = transform.Rotation.GetDir();
            // South is the default when deserializing.
            if (dir != Direction.South)
            {
                element.SetAttributeValue("direction", dir.ToString());
            }
            return element;
        }

        #endregion
    }
}
