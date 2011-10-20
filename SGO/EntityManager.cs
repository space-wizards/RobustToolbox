using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Xml.Linq;

namespace SGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager
    {
        private EntityFactory m_entityFactory;
        private EntityTemplateDatabase m_entityTemplateDatabase;
        private EntityNetworkManager m_entityNetworkManager;
        private NetServer m_netServer;

        private Dictionary<int, Entity> m_entities;
        public int lastId = 0;

        public EntityManager(NetServer netServer)
        {
            m_entityNetworkManager = new EntityNetworkManager(netServer);
            m_entityTemplateDatabase = new EntityTemplateDatabase();
            m_entityFactory = new EntityFactory(m_entityTemplateDatabase);
            m_entities = new Dictionary<int, Entity>();
            m_netServer = netServer;
            Singleton = this;
            LoadEntities();
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
        /// Load all entities from SavedEntities.xml
        /// </summary>
        public void LoadEntities()
        {
            XElement tmp = XDocument.Load("SavedEntities.xml").Element("SavedEntities");
            var SavedEntities = tmp.Descendants("SavedEntity");
            foreach (XElement e in SavedEntities)
            {
                LoadEntity(e);
            }
        }

        public void LoadEntity(XElement e)
        {
            float X = float.Parse(e.Attribute("X").Value);
            float Y = float.Parse(e.Attribute("Y").Value);
            string template = e.Attribute("template").Value;
            string name = e.Attribute("name").Value;
            Entity ent = SpawnEntity(template);
            ent.name = name;
            ent.Translate(new SS3D_shared.HelperClasses.Vector2(X, Y));
        }

        public void SaveEntities()
        {
            //List<XElement> entities = new List<XElement>();
            var entities = from e in m_entities.Values
                           where e.template.Name != "HumanMob"
                            select ToXML(e);

            XDocument saveFile = new XDocument(new XElement("SavedEntities", entities.ToArray()));
            saveFile.Save("SavedEntities.xml");
            
        }

        private XElement ToXML(Entity e)
        {
            XElement el = new XElement("SavedEntity", 
                new XAttribute("X", e.position.X.ToString()),
                new XAttribute("Y", e.position.Y.ToString()),
                new XAttribute("template", e.template.Name),
                new XAttribute("name", e.name));
            return el;
        }

        public void SendEntities(NetConnection client)
        {
            foreach (Entity e in m_entities.Values)
            {
                SendSpawnEntity(e, client);
            }
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
            e.SetNetworkManager(m_entityNetworkManager);
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

        public Entity SpawnEntity(string EntityType)
        {
            Entity e = m_entityFactory.CreateEntity(EntityType);
            if (e != null)
            {
                e.SetNetworkManager(m_entityNetworkManager);
                e.Uid = lastId++;
                m_entities.Add(e.Uid, e);
                e.Initialize();
                SendSpawnEntity(e);
            }
            return e;
        }

        private void SendSpawnEntity(Entity e)
        {
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityManagerMessage);
            message.Write((int)EntityManagerMessage.SpawnEntity);
            message.Write(e.name);
            message.Write(e.Uid);
            m_netServer.SendToAll(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void SendSpawnEntity(Entity e, NetConnection client)
        {
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityManagerMessage);
            message.Write((int)EntityManagerMessage.SpawnEntity);
            message.Write(e.name);
            message.Write(e.Uid);
            m_netServer.SendMessage(message, client, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// Adds an atom to the entity pool. Compatibility method.
        /// </summary>
        /// <param name="e">Entity to add</param>
        public void AddAtomEntity(Entity e)
        {
            ///The UID has already (in theory) been set in the atom manager.
            m_entities.Add(e.Uid, e);
            e.SetNetworkManager(m_entityNetworkManager);
        }

        public void Shutdown()
        {

        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg">Incoming raw network message</param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            IncomingEntityMessage message = m_entityNetworkManager.HandleEntityNetworkMessage(msg);
            m_entities[message.uid].HandleNetworkMessage(message);
        }

        #region Entity Manager Networking
        public void HandleNetworkMessage(NetIncomingMessage msg)
        {
            EntityManagerMessage type = (EntityManagerMessage)msg.ReadInt32();
            switch(type)
            {

            }
        }
        #endregion
        
    }
}
