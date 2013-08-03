using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.Network;
using GameObject;
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
        private EntityFactory _entityFactory;
        private bool _initialized;
        private int _lastId;

        private Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();

        public EntityTemplateDatabase TemplateDb { get; private set; }

        public EntityManager(INetworkManager networkManager)
            :base("CGO", new EntityNetworkManager(networkManager))
        {
            EngineType = EngineType.Client;
            TemplateDb = new EntityTemplateDatabase(this);
            _entityFactory = new EntityFactory(TemplateDb);
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

        private Entity SpawnEntity(string entityType, int uid)
        {
            var e = (Entity)_entityFactory.CreateEntity(entityType);
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

        private Entity SpawnEntityAt(string entityType, int uid, Vector2D position, Direction dir = Direction.North)
        {
            var e = SpawnEntity(entityType, uid);
            e.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction = dir;
            e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = position;
            return e;
        }
        
        public Entity[] GetEntitiesInRange(Vector2D position, float Range)
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
                    Entity e = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
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
