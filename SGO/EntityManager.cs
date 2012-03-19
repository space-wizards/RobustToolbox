using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Xml.Linq;
using SS13_Shared;
using System.Globalization;

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
        public int nextId = 0;

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
            float X = float.Parse(e.Attribute("X").Value, CultureInfo.InvariantCulture);
            float Y = float.Parse(e.Attribute("Y").Value, CultureInfo.InvariantCulture);
            string template = e.Attribute("template").Value;
            string name = e.Attribute("name").Value;
            Entity ent = SpawnEntity(template);
            ent.name = name;
            ent.Translate(new Vector2(X, Y));
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
                new XAttribute("X", e.position.X.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("Y", e.position.Y.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("template", e.template.Name),
                new XAttribute("name", e.name));
            return el;
        }

        public void SendEntities(NetConnection client)
        {
            foreach (Entity e in m_entities.Values)
            {
                SendSpawnEntityAtPosition(e, client);
            }
            SendEntityManagerInit(client);
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
        private int CreateEntity(string templateName)
        {
            //Get the entity from the factory
            Entity e = m_entityFactory.CreateEntity(templateName);
            e.SetNetworkManager(m_entityNetworkManager);
            if (e != null)
            {
                //It worked, add it.
                m_entities.Add(++nextId, e);
                return nextId;
            }
            //TODO: throw exception here -- something went wrong.
            return -1;
        }

        public Entity SpawnEntity(string EntityType, bool send = true)
        {
            Entity e = m_entityFactory.CreateEntity(EntityType);
            if (e != null)
            {
                e.SetNetworkManager(m_entityNetworkManager);
                e.Uid = nextId++;
                m_entities.Add(e.Uid, e);
                e.Initialize();
                if(send) SendSpawnEntity(e);
            }
            return e;
        }

        private Entity SpawnEntityAt(string EntityType, Vector2 position)
        {
            var e = SpawnEntity(EntityType, false);
            e.Translate(position);
            SendSpawnEntityAtPosition(e);
            return e;
        }

        public Entity SpawnEntity(string EntityType, Vector2 position)
        {
            Entity e = SpawnEntityAt(EntityType, position);
            if (e == null)
                return null;
            return e;
        }

        private void SendEntityManagerInit(NetConnection client)
        {
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityManagerMessage);
            message.Write((int)EntityManagerMessage.InitializeEntities);
            m_netServer.SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        private void SendSpawnEntity(Entity e, NetConnection client = null)
        {
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityManagerMessage);
            message.Write((int)EntityManagerMessage.SpawnEntity);
            message.Write(e.template.Name);
            message.Write(e.name);
            message.Write(e.Uid);
            if (client != null)
                m_netServer.SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
            else
                m_netServer.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void SendSpawnEntityAtPosition(Entity e, NetConnection client = null)
        {
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityManagerMessage);
            message.Write((int)EntityManagerMessage.SpawnEntityAtPosition);
            message.Write(e.template.Name);
            message.Write(e.name);
            message.Write(e.Uid);
            message.Write(e.position.X); // SENT AS DOUBLES...
            message.Write(e.position.Y); // CONVERTED TO FLOATS ON CLIENT SIDE
            if (client != null)
                m_netServer.SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
            else
                m_netServer.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public void DeleteEntity(Entity e)
        {
            e.Shutdown();
            NetOutgoingMessage message = m_netServer.CreateMessage();
            message.Write((byte)NetMessage.EntityManagerMessage);
            message.Write((int)EntityManagerMessage.DeleteEntity);
            message.Write(e.Uid);
            m_netServer.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
            m_entities.Remove(e.Uid);
        }

        public void Shutdown()
        {
            FlushEntities();
            m_entityFactory = null;
            m_entityTemplateDatabase = null;
            m_entityNetworkManager = null;
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
            var type = (EntityManagerMessage)msg.ReadInt32();
            switch(type)
            {

            }
        }
        #endregion
        
    }
}
