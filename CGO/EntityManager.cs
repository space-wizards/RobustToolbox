using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using ClientInterfaces.Network;
using Lidgren.Network;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager:GameObject.EntityManager
    {
        private readonly Dictionary<int, IEntity> _entities;

        private EntityFactory _entityFactory;
        public EntityNetworkManager EntityNetworkManager { get; private set; }
        private bool _initialized;
        private int _lastId;

        private Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();

        public EntityTemplateDatabase TemplateDb { get; private set; }

        public EntityManager(INetworkManager networkManager)
            :base("CGO")
        {
            EntityNetworkManager = new EntityNetworkManager(networkManager);
            TemplateDb = new EntityTemplateDatabase(this);
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

        public bool EntityExists(int eid)
        {
            return _entities.ContainsKey(eid);
        }

        private IEntity SpawnEntity(string entityType, int uid)
        {
            var e = _entityFactory.CreateEntity(entityType);
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

        private IEntity SpawnEntityAt(string entityType, int uid, Vector2D position, Direction dir = Direction.North)
        {
            var e = SpawnEntity(entityType, uid);
            e.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction = dir;
            e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = position;
            return e;
        }

        private void DeleteEntity(IEntity entity)
        {
            entity.Shutdown();
            _entities.Remove(entity.Uid);
        }

        private void DeleteEntity(int entityUid)
        {
            if(EntityExists(entityUid))
                DeleteEntity(GetEntity(entityUid));
        }

        public IEntity[] GetEntitiesInRange(Vector2D position, float Range)
        {
            var entities = from e in _entities.Values
                           where (position - e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position).Length < Range
                           select e;

            return entities.ToArray();
        }

        public void Shutdown()
        {
            FlushEntities();
            _entityFactory = null;
            TemplateDb = null;
            EntityNetworkManager = null;
            _initialized = false;
        }

        private void ProcessMsgBuffer()
        {
            if (!_initialized)
                return;
            if (!MessageBuffer.Any()) return;
            var misses = new List<IncomingEntityMessage>();

            while (MessageBuffer.Any())
            {
                IncomingEntityMessage entMsg = MessageBuffer.Dequeue();
                if (!_entities.ContainsKey(entMsg.Uid))
                {
                    entMsg.LastProcessingAttempt = DateTime.Now;
                    if ((entMsg.LastProcessingAttempt - entMsg.ReceivedTime).TotalSeconds > entMsg.Expires)
                        misses.Add(entMsg);
                }
                else
                    _entities[entMsg.Uid].HandleNetworkMessage(entMsg);
            }

            foreach (var miss in misses)
                MessageBuffer.Enqueue(miss);

            MessageBuffer.Clear(); //Should be empty at this point anyway.
        }

        private IncomingEntityMessage ProcessNetMessage(NetIncomingMessage msg)
        {
            return EntityNetworkManager.HandleEntityNetworkMessage(msg);
        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg">Incoming raw network message</param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            if (!_initialized)
            {
                var emsg = ProcessNetMessage(msg);
                if (emsg.MessageType != EntityMessage.Null)
                    MessageBuffer.Enqueue(emsg);
            }
            else
            {
                ProcessMsgBuffer();
                var emsg = ProcessNetMessage(msg);
                if (!_entities.ContainsKey(emsg.Uid))
                    MessageBuffer.Enqueue(emsg);
                else
                    _entities[emsg.Uid].HandleNetworkMessage(emsg);
            }
        }

        #region GameState Stuff
        public void ApplyEntityStates(List<EntityState> entityStates)
        {
            var entityKeys = new List<int>();
            foreach(var es in entityStates)
            {
                entityKeys.Add(es.StateData.Uid);
                //Known entities
                if(_entities.ContainsKey(es.StateData.Uid))
                {
                    _entities[es.StateData.Uid].HandleEntityState(es);
                }
                else //Unknown entities
                {
                    //SpawnEntityAt(es.StateData.TemplateName, es.StateData.Uid, es.StateData.Position);
                    IEntity e = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
                    e.Name = es.StateData.Name;
                    e.HandleEntityState(es);
                }
            }

            //Delete entities that exist here but don't exist in the entity states
            var toDelete = _entities.Keys.Where(k => !entityKeys.Contains(k)).ToArray();
            foreach(var k in toDelete) 
                DeleteEntity(k);

            if(!_initialized)
                InitializeEntities();
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
