using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager
    {
        private EntityFactory m_entityFactory;
        private EntityTemplateDatabase m_entityTemplateDatabase;
        private Dictionary<int, Entity> m_entities;
        private int lastId = 0;

        public EntityManager()
        {
            m_entityFactory = new EntityFactory();
            m_entityTemplateDatabase = new EntityTemplateDatabase();
        }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public Entity GetEntity(int eid)
        {
            if (m_entities.Keys.Contains(eid))
                return m_entities[eid];
            return null;
        }

        /// <summary>
        /// Creates an entity and adds it to the entity dictionary
        /// </summary>
        /// <param name="templateName">name of entity template to execute</param>
        /// <returns>integer id of added entity</returns>
        public int CreateEntity(string templateName)
        {
            EntityTemplate template = m_entityTemplateDatabase.GetTemplate(templateName);
            //TODO: Throw exception here
            if (template == null)
                return -1;
            Entity e = template.CreateEntity();
            if (e != null)
            {
                m_entities.Add(++lastId, e);
                lastId++;
                return lastId;
            }
            //TODO: throw exception here -- something went wrong.
            return -1;
        }
    }
}
