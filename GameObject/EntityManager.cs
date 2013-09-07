using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace GameObject
{
    public interface IEntityManager
    {
        ComponentFactory ComponentFactory { get; }
        ComponentManager ComponentManager { get; }
        IEntityNetworkManager EntityNetworkManager { get; set; }
        EngineType EngineType { get; set; }
        float Clock { get; }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        Entity GetEntity(int eid);

        List<Entity> GetEntities(EntityQuery query);

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        void DeleteEntity(Entity e);

        bool EntityExists(int eid);
    }

    public class EntityManager : IEntityManager
    {
        private readonly string _componentNamespace;
        protected readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        protected Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();
        protected int NextUid = 0;

        public EntityManager(EngineType engineType, IEntityNetworkManager entityNetworkManager)
        {
            EngineType = engineType;
            switch (EngineType)
            {
                case EngineType.Client:
                    _componentNamespace = "CGO";
                    break;
                case EngineType.Server:
                    _componentNamespace = "SGO";
                    break;
            }
            EntitySystemManager = new EntitySystemManager(this);
            ComponentFactory = new ComponentFactory(this, _componentNamespace);
            EntityNetworkManager = entityNetworkManager;
            ComponentManager = new ComponentManager();
            EntityTemplateDatabase = new EntityTemplateDatabase(this);
            EntityFactory = new EntityFactory(EntityTemplateDatabase);
            Clock = 0f;
            Initialize();
        }

        protected EntityFactory EntityFactory { get; private set; }
        public EntityTemplateDatabase EntityTemplateDatabase { get; private set; }
        public EntitySystemManager EntitySystemManager { get; private set; }
        public bool Initialized { get; protected set; }
        public float Clock { get; private set; }

        #region IEntityManager Members

        public ComponentFactory ComponentFactory { get; private set; }
        public ComponentManager ComponentManager { get; private set; }
        public IEntityNetworkManager EntityNetworkManager { get; set; }

        //This is a crude method to tell if we're running on the server or on the client. Fuck me.
        public EngineType EngineType { get; set; }

        #endregion

        public void Initialize()
        {
            switch (EngineType)
            {
                case EngineType.Client:
                    break;
                case EngineType.Server:
                    LoadEntities();
                    EntitySystemManager.Initialize();
                    Initialized = true;
                    InitializeEntities();
                    break;
            }
        }

        public virtual void InitializeEntities()
        {
            foreach (Entity e in _entities.Values.Where(e => !e.Initialized))
                e.Initialize();
            if (EngineType == EngineType.Client)
                Initialized = true;
        }

        public virtual void LoadEntities()
        {
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            EntityNetworkManager = null;
            EntitySystemManager.Shutdown();
            EntitySystemManager = null;
            Initialized = false;
        }

        public virtual void Update(float frameTime)
        {
            Clock += frameTime;
            EntitySystemManager.Update(frameTime);
        }

        #region Entity Management

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public Entity GetEntity(int eid)
        {
            return _entities.ContainsKey(eid) ? _entities[eid] : null;
        }


        public List<Entity> GetEntities(EntityQuery query)
        {
            return _entities.Values.Where(e => e.Match(query)).ToList();
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public void DeleteEntity(Entity e)
        {
            e.Shutdown();
            _entities.Remove(e.Uid);
        }

        public bool EntityExists(int eid)
        {
            return _entities.ContainsKey(eid);
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

        protected void DeleteEntity(int entityUid)
        {
            if (EntityExists(entityUid))
                DeleteEntity(GetEntity(entityUid));
        }

        /// <summary>
        /// Creates an entity and adds it to the entity dictionary
        /// </summary>
        /// <param name="EntityType">name of entity template to execute</param>
        /// <returns>spawned entity</returns>
        public Entity SpawnEntity(string EntityType, int uid = -1)
        {
            if (uid == -1)
                uid = NextUid++;
            Entity e = EntityFactory.CreateEntity(EntityType);
            if (e != null)
            {
                e.Uid = uid;
                _entities.Add(e.Uid, e);
                if (Initialized)
                    e.Initialize();
                if (!e.HasComponent(ComponentFamily.SVars))
                    e.AddComponent(ComponentFamily.SVars, ComponentFactory.GetComponent("SVarsComponent"));
            }
            return e;
        }

        #endregion

        #region message processing

        protected void ProcessMsgBuffer()
        {
            if (!Initialized)
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

            foreach (IncomingEntityMessage miss in misses)
                MessageBuffer.Enqueue(miss);

            MessageBuffer.Clear(); //Should be empty at this point anyway.
        }

        protected IncomingEntityMessage ProcessNetMessage(NetIncomingMessage msg)
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
            if (!Initialized)
            {
                IncomingEntityMessage emsg = ProcessNetMessage(msg);
                if (emsg.MessageType != EntityMessage.Null)
                    MessageBuffer.Enqueue(emsg);
            }
            else
            {
                ProcessMsgBuffer();
                IncomingEntityMessage emsg = ProcessNetMessage(msg);
                if (!_entities.ContainsKey(emsg.Uid))
                    MessageBuffer.Enqueue(emsg);
                else
                    _entities[emsg.Uid].HandleNetworkMessage(emsg);
            }
        }

        #endregion

        #region State stuff

        public void ApplyEntityStates(List<EntityState> entityStates, float serverTime)
        {
            var entityKeys = new List<int>();
            foreach (EntityState es in entityStates)
            {
                es.ReceivedTime = serverTime;
                entityKeys.Add(es.StateData.Uid);
                //Known entities
                if (_entities.ContainsKey(es.StateData.Uid))
                {
                    _entities[es.StateData.Uid].HandleEntityState(es);
                }
                else //Unknown entities
                {
                    Entity e = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
                    e.Name = es.StateData.Name;
                    e.HandleEntityState(es);
                }
            }

            //Delete entities that exist here but don't exist in the entity states
            int[] toDelete = _entities.Keys.Where(k => !entityKeys.Contains(k)).ToArray();
            foreach (int k in toDelete)
                DeleteEntity(k);

            if (!Initialized)
                InitializeEntities();
        }

        #endregion
    }
}