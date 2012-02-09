using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.GOC;
using ClientInterfaces.Network;
using Lidgren.Network;
using GorgonLibrary;
using SS13_Shared;

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
        private bool initialized = false;

        public EntityTemplateDatabase TemplateDB { get { return m_entityTemplateDatabase; } }

        private Dictionary<int, IEntity> m_entities;
        private int lastId = 0;

        public EntityManager(INetworkManager networkManager)
        {
            m_entityNetworkManager = new EntityNetworkManager(networkManager);
            m_entityTemplateDatabase = new EntityTemplateDatabase();
            m_entityFactory = new EntityFactory(m_entityTemplateDatabase);
            m_entities = new Dictionary<int, IEntity>();
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
        /// Disposes all entities and clears all lists.
        /// </summary>
        public void FlushEntities()
        {
            foreach (Entity e in m_entities.Values)
                e.Shutdown();
            m_entities.Clear();
        }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public IEntity GetEntity(int eid)
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
                e.SetNetworkManager(m_entityNetworkManager);
                m_entities.Add(++lastId, e);
                lastId++;
                return lastId;
            }
            //TODO: throw exception here -- something went wrong.
            return -1;
        }

        private IEntity SpawnEntity(string entityType, int uid)
        {

            var e = m_entityFactory.CreateEntity(entityType);
            if (e != null)
            {
                e.SetNetworkManager(m_entityNetworkManager);
                e.Uid = uid;
                m_entities.Add(uid, e);
                lastId = uid;
                if(initialized)
                    e.Initialize();
                return e;
            }
            return null;
        }

        public IEntity[] GetEntitiesInRange(Vector2D position, float Range)
        {
            var entities = from e in m_entities.Values
                           where (position - e.Position).Length < Range
                           select e;

            return entities.ToArray();
        }

        public void Shutdown()
        {
            FlushEntities();
            m_entityFactory = null;
            m_entityTemplateDatabase = null;
            m_entityNetworkManager = null;
        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg"></param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            var message = m_entityNetworkManager.HandleEntityNetworkMessage(msg);
            m_entities[message.Uid].HandleNetworkMessage(message);
        }

        #region Entity Manager Networking
        public void HandleNetworkMessage(NetIncomingMessage msg)
        {
            var type = (EntityManagerMessage)msg.ReadInt32();
            switch(type)
            {
                case EntityManagerMessage.SpawnEntity:
                    var entityType = msg.ReadString();
                    var entityName = msg.ReadString();
                    var uid = msg.ReadInt32();
                    var e = SpawnEntity(entityType, uid);
                    e.Name = entityName;
                    break;
                case EntityManagerMessage.DeleteEntity:
                    var dUid = msg.ReadInt32();
                    var ent = GetEntity(dUid);
                    if (ent != null)
                    {
                        ent.Shutdown();
                        m_entities.Remove(dUid);
                    }
                    break;
                case EntityManagerMessage.InitializeEntities:
                    InitializeEntities();
                    break;
            }
        }

        private void InitializeEntities()
        {
            foreach (var e in m_entities.Values)
                e.Initialize();
            initialized = true;
        }

        #endregion
    }
}
