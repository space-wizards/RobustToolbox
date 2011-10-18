using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace CGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager
    {
        private EntityFactory m_entityFactory;
        private EntityTemplateDatabase m_entityTemplateDatabase;
        private EntityNetworkManager m_entityNetworkManager;
        private Dictionary<int, Entity> m_entities;
        private int lastId = 0;

        public EntityManager(NetClient netClient)
        {
            m_entityNetworkManager = new EntityNetworkManager(netClient);
            m_entityTemplateDatabase = new EntityTemplateDatabase();
            m_entityFactory = new EntityFactory(m_entityTemplateDatabase);
            m_entities = new Dictionary<int, Entity>();
            Singleton = this;
        }

        private static EntityManager singleton;
        public static EntityManager Singleton
        {
            get
            {
                if (singleton == null)
                    throw new Exception("Singleton not initialized");
                else return singleton;
            }
            set
            { singleton = value; }
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
            //Get the entity from the factory
            Entity e = m_entityFactory.CreateEntity(templateName);
            if (e != null)
            {
                //It worked, add it.
                m_entities.Add(++lastId, e);
                lastId++;
                return lastId;
            }
            //TODO: throw exception here -- something went wrong.
            return -1;
        }

        /// <summary>
        /// Adds an atom to the entity pool. Compatibility method.
        /// </summary>
        /// <param name="e">Entity to add</param>
        public void AddAtomEntity(Entity e)
        {
            ///The UID has already been set by the server..
            m_entities.Add(e.Uid, e);
            e.SetNetworkManager(m_entityNetworkManager);
        }

        /// <summary>
        /// Returns an entity created using an atom name.
        /// </summary>
        /// <param name="atomName"></param>
        /// <returns></returns>
        public Entity TryCreateAtom(string atomName)
        {
            EntityTemplate template = m_entityTemplateDatabase.GetTemplateByAtomName(atomName);
            if (template != null)
            {
                Entity e = template.CreateEntity();
                return e;
            }
            return null;
        }

        public void Shutdown()
        {

        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg"></param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            IncomingEntityMessage message = m_entityNetworkManager.HandleEntityNetworkMessage(msg);
            m_entities[message.uid].HandleNetworkMessage(message);
        }

        public void HandleNetworkMessage(NetIncomingMessage msg)
        {

        }
    }
}
