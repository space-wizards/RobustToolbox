using SS14.Server.Interfaces.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SS14.Shared.Maths;
using IEntityManager = SS14.Server.Interfaces.GOC.IEntityManager;
using SFML.System;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager : SS14.Shared.GameObjects.EntityManager, IEntityManager
    {
        public EntityManager(ISS14NetServer netServer)
            : base(EngineType.Server, new EntityNetworkManager(netServer))
        {
        }

        #region IEntityManager Members

        public void SaveEntities()
        {
            //List<XElement> entities = new List<XElement>();
            IEnumerable<XElement> entities = from Entity e in _entities.Values
                                             where e.Template.Name != "HumanMob"
                                             select ToXML(e);

            var saveFile = new XDocument(new XElement("SavedEntities", entities.ToArray()));
            saveFile.Save("SavedEntities.xml");
        }

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity SpawnEntityAt(string EntityType, Vector2f position)
        {
            Entity e = SpawnEntity(EntityType);
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
        
        #endregion

        /// <summary>
        /// Load all entities from SavedEntities.xml
        /// </summary>
        public override void LoadEntities()
        {
            XElement tmp;
            try
            {
                tmp = XDocument.Load("SavedEntities.xml").Element("SavedEntities");
            }
            catch (FileNotFoundException e)
            {
                var saveFile = new XDocument(new XElement("SavedEntities"));
                saveFile.Save("SavedEntities.xml");
                tmp = XDocument.Load("SavedEntities.xml").Element("SavedEntities");
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
                dir = (Direction) Enum.Parse(typeof (Direction), e.Attribute("direction").Value, true);

            string template = e.Attribute("template").Value;
            string name = e.Attribute("name").Value;
            Entity ent = SpawnEntity(template);
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
                                  new XAttribute("template", e.Template.Name),
                                  new XAttribute("name", e.Name),
                                  new XAttribute("direction",
                                                 e.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction
                                                     .ToString()));
            return el;
        }
    }
}