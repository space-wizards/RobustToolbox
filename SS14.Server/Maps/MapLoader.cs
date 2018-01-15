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
using System.Xml.Linq;

namespace SS14.Server.Maps
{
    public class MapLoader : IMapLoader
    {
        [Dependency]
        private IResourceManager resourceManager;

        [Dependency]
        private IServerEntityManager entityManager;

        public void Load(string filename, IMap map)
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

        public void Save(string filename, IMap map)
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
    }
}
