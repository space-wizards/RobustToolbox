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
        #region Dependencies
        protected readonly IEntityNetworkManager EntityNetworkManager;
        protected readonly IPrototypeManager PrototypeManager;
        protected readonly IEntitySystemManager EntitySystemManager;
        protected readonly IComponentFactory ComponentFactory;
        protected readonly IComponentManager ComponentManager;
        # endregion Dependencies

        protected readonly Dictionary<int, IEntity> _entities = new Dictionary<int, IEntity>();
        protected Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();
        protected int NextUid = 0;

        private Dictionary<Type, List<Delegate>> _eventSubscriptions
            = new Dictionary<Type, List<Delegate>>();
        private Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>>();

        private Queue<Tuple<object, EntityEventArgs>> _eventQueue
            = new Queue<Tuple<object, EntityEventArgs>>();

        public IList<ComponentFamily> SynchedComponentTypes => synchedComponentTypes;

        private readonly IList<ComponentFamily> synchedComponentTypes = new List<ComponentFamily>
        {
            ComponentFamily.Mover
        };

        public EntityManager()
        {
            EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            ComponentFactory = IoCManager.Resolve<IComponentFactory>();
            EntityNetworkManager = IoCManager.Resolve<IEntityNetworkManager>();
            ComponentManager = IoCManager.Resolve<IComponentManager>();
            PrototypeManager = IoCManager.Resolve<IPrototypeManager>();
        }

        public bool Initialized { get; protected set; }

        #region IEntityManager Members

        public virtual void InitializeEntities()
        {
            foreach (IEntity e in _entities.Values.Where(e => !e.Initialized))
            {
                e.Initialize();
            }
        }

        public virtual void LoadEntities()
        {
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            EntitySystemManager.Shutdown();
            Initialized = false;
        }

        public virtual void Update(float frameTime)
        {
            EntitySystemManager.Update(frameTime);
            ProcessEventQueue();
        }

        /// <summary>
        /// Retrieves template with given name from prototypemanager.
        /// </summary>
        /// <param name="prototypeName">name of the template</param>
        /// <returns>Template</returns>
        public EntityPrototype GetTemplate(string prototypeName)
        {
            return PrototypeManager.Index<EntityPrototype>(prototypeName);
        }

        #region Entity Management

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public IEntity GetEntity(int eid)
        {
            return _entities.ContainsKey(eid) ? _entities[eid] : null;
        }

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="eid">The entity ID to look up.</param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        public bool TryGetEntity(int eid, out IEntity entity)
        {
            entity = GetEntity(eid);

            return entity != null;
        }

        public IEnumerable<IEntity> GetEntities(IEntityQuery query)
        {
            return _entities.Values.Where(e => e.Match(query));
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public void DeleteEntity(IEntity e)
        {
            e.Shutdown();
            _entities.Remove(e.Uid);
        }

        public void DeleteEntity(int entityUid)
        {
            if (EntityExists(entityUid))
            {
                DeleteEntity(GetEntity(entityUid));
            }
            else
            {
                throw new ArgumentException(string.Format("No entity with ID {0} exists.", entityUid));
            }
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
            foreach (IEntity e in _entities.Values)
            {
                e.Shutdown();
            }
            _entities.Clear();
        }

        /// <summary>
        /// Creates an entity and adds it to the entity dictionary
        /// </summary>
        /// <param name="prototypeName">name of entity template to execute</param>
        /// <returns>spawned entity</returns>
        public IEntity SpawnEntity(string prototypeName, int uid = -1)
        {
            if (uid == -1)
            {
                uid = NextUid++;
            }

            EntityPrototype prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            IEntity e = prototype.CreateEntity(this, EntityNetworkManager, ComponentFactory);

            e.Uid = uid;
            _entities.Add(e.Uid, e);
            if (!Initialized)
            {
                e.Initialize();
            }
            if (!e.HasComponent(ComponentFamily.SVars))
            {
                e.AddComponent(ComponentFamily.SVars, ComponentFactory.GetComponent("SVars"));
            }
            return e;
        }

        #endregion Entity Management

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
                    IEntity e = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
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

        #endregion IEntityManager Members

        #region ComponentEvents

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

        #endregion ComponentEvents

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
    }
}
