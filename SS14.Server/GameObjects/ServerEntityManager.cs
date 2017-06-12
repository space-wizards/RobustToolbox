using SFML.System;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    [IoCTarget(Priority = 5)]
    public class ServerEntityManager : EntityManager, IServerEntityManager
    {
        #region IEntityManager Members

        public void SaveEntities()
        {
            //List<XElement> entities = new List<XElement>();
            IEnumerable<XElement> entities = from Entity e in _entities.Values
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
        public IEntity SpawnEntityAt(string EntityType, Vector2f position)
        {
            IEntity e = SpawnEntity(EntityType);
            e.GetComponent<TransformComponent>(ComponentFamily.Transform).TranslateTo(position);
            e.Initialize();
            return e;
        }

        public List<EntityState> GetEntityStates()
        {
            var stateEntities = new List<EntityState>();
            foreach (Entity entity in _entities.Values)
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
            ent.GetComponent<TransformComponent>(ComponentFamily.Transform).TranslateTo(new Vector2f(X, Y));
            ent.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction = dir;
        }

        private XElement ToXML(Entity e)
        {
            var el = new XElement("SavedEntity",
                                  new XAttribute("X",
                                                 e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.
                                                     X.ToString(CultureInfo.InvariantCulture)),
                                  new XAttribute("Y",
                                                 e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.
                                                     Y.ToString(CultureInfo.InvariantCulture)),
                                  new XAttribute("template", e.Prototype.ID),
                                  new XAttribute("name", e.Name),
                                  new XAttribute("direction",
                                                 e.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction
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
