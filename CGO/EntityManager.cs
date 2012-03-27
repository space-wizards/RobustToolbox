using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<int, IEntity> _entities;

        private EntityFactory _entityFactory;
        private EntityNetworkManager _entityNetworkManager;
        private bool _initialized;
        private int _lastId;

        public EntityTemplateDatabase TemplateDb { get; private set; }

        public EntityManager(INetworkManager networkManager)
        {
            _entityNetworkManager = new EntityNetworkManager(networkManager);
            TemplateDb = new EntityTemplateDatabase();
            _entityFactory = new EntityFactory(TemplateDb);
            _entities = new Dictionary<int, IEntity>();
            Singleton = this;
        }

        private static EntityManager singleton;
        public static EntityManager Singleton
        {
            get
            {
                if (singleton == null) throw new Exception("Singleton not initialized");

                return singleton;
            }
            set
            { singleton = value; }
        }

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        public void FlushEntities()
        {
            foreach (Entity e in _entities.Values)
                e.Shutdown();
            _entities.Clear();
        }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public IEntity GetEntity(int eid)
        {
            return _entities.Keys.Contains(eid) ? _entities[eid] : null;
        }

        /// <summary>
        /// Creates an entity and adds it to the entity dictionary
        /// </summary>
        /// <param name="templateName">name of entity template to execute</param>
        /// <returns>integer id of added entity</returns>
        public int CreateEntity(string templateName)
        {
            //Get the entity from the factory
            var e = _entityFactory.CreateEntity(templateName, _entityNetworkManager);
            if (e != null)
            {
                //It worked, add it.
                _entities.Add(++_lastId, e);
                _lastId++;
                return _lastId;
            }
            //TODO: throw exception here -- something went wrong.
            return -1;
        }

        private IEntity SpawnEntity(string entityType, int uid)
        {
            var e = _entityFactory.CreateEntity(entityType, _entityNetworkManager);
            if (e != null)
            {
                e.Uid = uid;
                _entities.Add(uid, e);
                _lastId = uid;
                if(_initialized)
                    e.Initialize();
                return e;
            }
            return null;
        }

        private IEntity SpawnEntityAt(string entityType, int uid, Vector2D position)
        {
            var e = SpawnEntity(entityType, uid);
            e.Position = position;
            return e;
        }

        public IEntity[] GetEntitiesInRange(Vector2D position, float Range)
        {
            var entities = from e in _entities.Values
                           where (position - e.Position).Length < Range
                           select e;

            return entities.ToArray();
        }

        public void Shutdown()
        {
            FlushEntities();
            _entityFactory = null;
            TemplateDb = null;
            _entityNetworkManager = null;
        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg"></param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            /**
             * IF we haven't loaded all of the entities yet we should ignore messages.
             * BUT we might still need some of those messages at some point, because they
             * may be important once we're initialized.
             * TODO: Write a message caching func.
             */
            if (!_initialized) 
                return;
            var message = _entityNetworkManager.HandleEntityNetworkMessage(msg);
            _entities[message.Uid].HandleNetworkMessage(message);
        }

        #region Entity Manager Networking
        public void HandleNetworkMessage(NetIncomingMessage msg)
        {
            var type = (EntityManagerMessage)msg.ReadInt32();
            switch(type)
            {
                case EntityManagerMessage.SpawnEntity:
                    HandleSpawnEntity(msg);
                    break;
                case EntityManagerMessage.SpawnEntityAtPosition:
                    HandleSpawnEntityAtPosition(msg);
                    break;
                case EntityManagerMessage.DeleteEntity:
                    var dUid = msg.ReadInt32();
                    var ent = GetEntity(dUid);
                    if (ent != null)
                    {
                        ent.Shutdown();
                        _entities.Remove(dUid);
                    }
                    break;
                case EntityManagerMessage.InitializeEntities:
                    InitializeEntities();
                    break;
            }
        }

        private void HandleSpawnEntity(NetIncomingMessage msg)
        {
            var entityType = msg.ReadString();
            var entityName = msg.ReadString();
            var uid = msg.ReadInt32();
            var e = SpawnEntity(entityType, uid);
            e.Name = entityName;
        }

        private void HandleSpawnEntityAtPosition(NetIncomingMessage msg)
        {
            var entityType = msg.ReadString();
            var entityName = msg.ReadString();
            var uid = msg.ReadInt32();
            var pos = new Vector2D((float)msg.ReadDouble(), (float)msg.ReadDouble());
            var e = SpawnEntityAt(entityType, uid, pos);
            e.Name = entityName;
        }

        private void InitializeEntities()
        {
            foreach (var e in _entities.Values)
                e.Initialize();
            _initialized = true;
        }

        #endregion
    }
}
