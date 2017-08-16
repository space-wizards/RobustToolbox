using SFML.System;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenTK;
using SS14.Shared.ContentPack;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ServerEntityManager : EntityManager, IServerEntityManager
    {
        #region IEntityManager Members

        public void SaveEntities()
        {
            IEnumerable<XElement> entities = from IEntity e in _entities.Values
                                             where e.Prototype.ID != "HumanMob"
                                             select ToXML(e);

            var saveFile = new XDocument(new XElement("SavedEntities", entities.ToArray()));
            saveFile.Save(PathHelpers.ExecutableRelativeFile("SavedEntities.xml"));
        }

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public IEntity SpawnEntityAt(string EntityType, Vector2 position)
        {
            IEntity e = SpawnEntity(EntityType);
            e.GetComponent<TransformComponent>().Position = position.Convert();
            e.Initialize();
            return e;
        }

        public List<EntityState> GetEntityStates()
        {
            var stateEntities = new List<EntityState>();
            foreach (IEntity entity in _entities.Values)
            {
                EntityState entityState = entity.GetEntityState();
                stateEntities.Add(entityState);
            }
            return stateEntities;
        }

        #endregion IEntityManager Members

        /// <summary>
        /// Load all entities from SavedEntities.xml
        /// </summary>
        public override void LoadEntities()
        {
            XElement tmp;
            try
            {
                tmp = XDocument.Load(PathHelpers.ExecutableRelativeFile("SavedEntities.xml")).Element("SavedEntities");
            }
            catch (FileNotFoundException)
            {
                var saveFile = new XDocument(new XElement("SavedEntities"));
                saveFile.Save(PathHelpers.ExecutableRelativeFile("SavedEntities.xml"));
                tmp = XDocument.Load(PathHelpers.ExecutableRelativeFile("SavedEntities.xml")).Element("SavedEntities");
            }
            IEnumerable<XElement> SavedEntities = tmp.Descendants("SavedEntity");
            foreach (XElement e in SavedEntities)
            {
                LoadEntity(e);
            }
        }

        public void LoadEntity(XElement e)
        {
            float X = float.Parse(e.Attribute("X").Value, CultureInfo.InvariantCulture);
            float Y = float.Parse(e.Attribute("Y").Value, CultureInfo.InvariantCulture);

            var dir = Direction.South;
            if (e.Attribute("direction") != null)
                dir = (Direction)Enum.Parse(typeof(Direction), e.Attribute("direction").Value, true);

            string template = e.Attribute("template").Value;
            string name = e.Attribute("name").Value;
            IEntity ent = SpawnEntity(template);
            ent.Name = name;
            ent.GetComponent<TransformComponent>().Position = new Vector2(X, Y);
            ent.GetComponent<TransformComponent>().Rotation = (float) dir.ToAngle();
        }

        private XElement ToXML(IEntity e)
        {
            var el = new XElement("SavedEntity",
                                  new XAttribute("X",
                                                 e.GetComponent<ITransformComponent>().Position.
                                                     X.ToString(CultureInfo.InvariantCulture)),
                                  new XAttribute("Y",
                                                 e.GetComponent<ITransformComponent>().Position.
                                                     Y.ToString(CultureInfo.InvariantCulture)),
                                  new XAttribute("template", e.Prototype.ID),
                                  new XAttribute("name", e.Name),
                                  new XAttribute("direction",
                                                 e.GetComponent<TransformComponent>().Rotation.GetDir()
                                                     .ToString()));
            return el;
        }

        public void Initialize()
        {
            LoadEntities();
            EntitySystemManager.Initialize();
            Initialized = true;
            InitializeEntities();
        }
    }
}
