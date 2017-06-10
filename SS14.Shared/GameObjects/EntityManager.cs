using Lidgren.Network;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    [IoCTarget]
    public class EntityManager : IEntityManager
    {
        protected readonly IEntityNetworkManager entityNetworkManager;

        protected readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        protected Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();
        protected int NextUid = 0;

        private Dictionary<Type, List<Delegate>> _eventSubscriptions
            = new Dictionary<Type, List<Delegate>>();
        private Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>>();

        private Queue<Tuple<object, EntityEventArgs>> _eventQueue
            = new Queue<Tuple<object, EntityEventArgs>>();

        public readonly List<ComponentFamily> SynchedComponentTypes = new List<ComponentFamily>
                                                                  {
                                                                      ComponentFamily.Mover
                                                                  };

        public EntityManager(IEntityNetworkManager entityNetworkManager)
        {
            EntitySystemManager = new EntitySystemManager(this);
            ComponentFactory = new ComponentFactory(this);
            EntityNetworkManager = entityNetworkManager;
            ComponentManager = new ComponentManager();
            PrototypeManager = IoCManager.Resolve<IPrototypeManager>();
            EntityFactory = new EntityFactory(PrototypeManager, this);
            Clock = 0f;
            Initialize();
        }

        protected EntityFactory EntityFactory { get; private set; }
        public IPrototypeManager PrototypeManager { get; private set; }
        public EntitySystemManager EntitySystemManager { get; private set; }
        public bool Initialized { get; protected set; }
        public float Clock { get; private set; }

        #region IEntityManager Members

        public ComponentFactory ComponentFactory { get; private set; }
        public ComponentManager ComponentManager { get; private set; }
        public IEntityNetworkManager EntityNetworkManager { get; set; }

        #endregion IEntityManager Members

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
            ProcessEventQueue();
        }

        #region ComponentEvents

        public void SubscribeEvent<T>(Delegate eventHandler, IEntityEventSubscriber s) where T : EntityEventArgs
        {
            Type eventType = typeof(T);
            //var evh = (ComponentEventHandler<ComponentEventArgs>)Convert.ChangeType(eventHandler, typeof(ComponentEventHandler<ComponentEventArgs>));
            if (!_eventSubscriptions.ContainsKey(eventType))
            {
                _eventSubscriptions.Add(eventType, new List<Delegate> { eventHandler });
            }
            else if (!_eventSubscriptions[eventType].Contains(eventHandler))
            {
                _eventSubscriptions[eventType].Add(eventHandler);
            }
            if (!_inverseEventSubscriptions.ContainsKey(s))
            {
                _inverseEventSubscriptions.Add(
                    s,
                    new Dictionary<Type, Delegate>()
                );
            }
            if (!_inverseEventSubscriptions[s].ContainsKey(eventType))
            {
                _inverseEventSubscriptions[s].Add(eventType, eventHandler);
            }
        }

        public void UnsubscribeEvent<T>(IEntityEventSubscriber s) where T : EntityEventArgs
        {
            Type eventType = typeof(T);

            if (_inverseEventSubscriptions.ContainsKey(s) && _inverseEventSubscriptions[s].ContainsKey(eventType))
            {
                UnsubscribeEvent(eventType, _inverseEventSubscriptions[s][eventType], s);
            }
        }

        public void UnsubscribeEvent(Type eventType, Delegate evh, IEntityEventSubscriber s)
        {
            if (_eventSubscriptions.ContainsKey(eventType) && _eventSubscriptions[eventType].Contains(evh))
            {
                _eventSubscriptions[eventType].Remove(evh);
            }
            if (_inverseEventSubscriptions.ContainsKey(s) && _inverseEventSubscriptions[s].ContainsKey(eventType))
            {
                _inverseEventSubscriptions[s].Remove(eventType);
            }
        }

        public void RaiseEvent(object sender, EntityEventArgs toRaise)
        {
            _eventQueue.Enqueue(new Tuple<object, EntityEventArgs>(sender, toRaise));
        }

        private void ProcessEventQueue()
        {
            while (_eventQueue.Any())
            {
                var current = _eventQueue.Dequeue();
                ProcessSingleEvent(current);
            }
        }

        private void ProcessSingleEvent(Tuple<object, EntityEventArgs> argsTuple)
        {
            var sender = argsTuple.Item1;
            var args = argsTuple.Item2;
            var eventType = args.GetType();
            if (_eventSubscriptions.ContainsKey(eventType))
            {
                foreach (Delegate handler in _eventSubscriptions[eventType])
                {
                    handler.DynamicInvoke(sender, args);
                }
            }
        }

        public void RemoveSubscribedEvents(IEntityEventSubscriber subscriber)
        {
            if (_inverseEventSubscriptions.ContainsKey(subscriber))
            {
                foreach (KeyValuePair<Type, Delegate> keyValuePair in _inverseEventSubscriptions[subscriber])
                {
                    UnsubscribeEvent(keyValuePair.Key, keyValuePair.Value, subscriber);
                }
            }
        }
        #endregion ComponentEvents

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
                if (!Initialized)
                    e.Initialize();
                if (!e.HasComponent(ComponentFamily.SVars))
                    e.AddComponent(ComponentFamily.SVars, ComponentFactory.GetComponent("SVars"));
            }
            return e;
        }

        #endregion Entity Management

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
                {
                    MessageBuffer.Enqueue(emsg);
                }
                else
                    _entities[emsg.Uid].HandleNetworkMessage(emsg);
            }
        }

        #endregion message processing

        #region State stuff

        public void ApplyEntityStates(List<EntityState> entityStates, float serverTime)
        {
            var entityKeys = new List<int>();
            foreach (EntityState es in entityStates)
            {
                //Todo defer component state result processing until all entities are loaded and initialized...
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

        #endregion State stuff
    }
}
