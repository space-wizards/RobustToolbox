using Lidgren.Network;
using OpenTK;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Shared.GameObjects
{
    public class EntityManager : IEntityManager
    {
        #region Dependencies
        [Dependency]
        protected readonly IEntityNetworkManager EntityNetworkManager;
        [Dependency]
        protected readonly IPrototypeManager PrototypeManager;
        [Dependency]
        protected readonly IEntitySystemManager EntitySystemManager;
        [Dependency]
        protected readonly IComponentFactory ComponentFactory;
        [Dependency]
        protected readonly IComponentManager ComponentManager;
        # endregion Dependencies

        protected readonly Dictionary<int, IEntity> _entities = new Dictionary<int, IEntity>();
        /// <summary>
        /// List of all entities, used for iteration.
        /// </summary>
        private readonly List<IEntity> _allEntities = new List<IEntity>();
        protected Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();
        protected int NextUid = 0;

        private Dictionary<Type, List<Delegate>> _eventSubscriptions
            = new Dictionary<Type, List<Delegate>>();
        private Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>>();

        private Queue<Tuple<object, EntityEventArgs>> _eventQueue
            = new Queue<Tuple<object, EntityEventArgs>>();

        public bool Initialized { get; protected set; }
        public bool MapsInitialized { get; set; } = false;

        #region IEntityManager Members

        public virtual void Shutdown()
        {
            FlushEntities();
            EntitySystemManager.Shutdown();
            Initialized = false;
            var componentmanager = IoCManager.Resolve<IComponentManager>();
            componentmanager.Cull();
        }

        public virtual void Update(float frameTime)
        {
            EntitySystemManager.Update(frameTime);
            ProcessEventQueue();
            CullDeletedEntities();
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
            TryGetEntity(eid, out var entity);
            return entity;
        }

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="eid">The entity ID to look up.</param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        public bool TryGetEntity(int eid, out IEntity entity)
        {
            if (_entities.TryGetValue(eid, out entity) && !entity.Deleted)
            {
                return true;
            }

            // entity might get assigned if it's deleted but still found,
            // prevent somebody from being "smart".
            entity = null;
            return false;
        }

        public IEnumerable<IEntity> GetEntities(IEntityQuery query)
        {
            return GetEntities().Where(e => e.Match(query));
        }

        public IEnumerable<IEntity> GetEntitiesAt(Vector2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.GetComponent<ITransformComponent>();
                if (FloatMath.CloseTo(transform.LocalPosition.X, position.X) && FloatMath.CloseTo(transform.LocalPosition.Y, position.Y))
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<IEntity> GetEntities()
        {
            return _allEntities.Where(e => !e.Deleted);
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public void DeleteEntity(IEntity e)
        {
            e.Shutdown();
        }

        public void DeleteEntity(int entityUid)
        {
            if (TryGetEntity(entityUid, out var entity))
            {
                DeleteEntity(entity);
            }
            else
            {
                throw new ArgumentException(string.Format("No entity with ID {0} exists.", entityUid));
            }
        }

        public bool EntityExists(int eid)
        {
            return TryGetEntity(eid, out var _);
        }

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        public void FlushEntities()
        {
            foreach (IEntity e in GetEntities())
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
        public IEntity SpawnEntity(string prototypeName, int? _uid = null)
        {
            int uid;
            if (_uid == null)
            {
                uid = NextUid++;
            }
            else
            {
                uid = _uid.Value;
            }
            if (EntityExists(uid))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            EntityPrototype prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            IEntity entity = prototype.CreateEntity(uid, this, EntityNetworkManager, ComponentFactory);
            _entities[uid] = entity;
            _allEntities.Add(entity);

            // We batch the first set of initializations together.
            if (Initialized)
            {
                InitializeEntity(entity);
            }

            return entity;
        }

        private void InitializeEntity(IEntity entity)
        {
            entity.PreInitialize();
            foreach (var component in entity.GetComponents())
            {
                component.Initialize();
            }

            entity.Initialize();

            foreach (var component in entity.GetComponents())
            {
                component.Startup();
            }
        }

        /// <summary>
        /// Initializes all entities that haven't been initialized yet.
        /// </summary>
        protected void InitializeEntities()
        {
            foreach (var entity in GetEntities().Where(e => !e.Initialized))
            {
                InitializeEntity(entity);
            }
        }

        #endregion Entity Management

        #region ComponentEvents

        public void SubscribeEvent<T>(Delegate eventHandler, IEntityEventSubscriber s) where T : EntityEventArgs
        {
            Type eventType = typeof(T);
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
                IncomingEntityMessage incomingEntity = MessageBuffer.Dequeue();
                if (!_entities.ContainsKey(incomingEntity.Message.EntityId))
                {
                    incomingEntity.LastProcessingAttempt = DateTime.Now;
                    if ((incomingEntity.LastProcessingAttempt - incomingEntity.ReceivedTime).TotalSeconds > incomingEntity.Expires)
                        misses.Add(incomingEntity);
                }
                else
                    _entities[incomingEntity.Message.EntityId].HandleNetworkMessage(incomingEntity);
            }

            foreach (IncomingEntityMessage miss in misses)
                MessageBuffer.Enqueue(miss);

            MessageBuffer.Clear(); //Should be empty at this point anyway.
        }

        protected IncomingEntityMessage ProcessNetMessage(MsgEntity msg)
        {
            return EntityNetworkManager.HandleEntityNetworkMessage(msg);
        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg">Incoming raw network message</param>
        public void HandleEntityNetworkMessage(MsgEntity msg)
        {
            if (!Initialized)
            {
                IncomingEntityMessage incomingEntity = ProcessNetMessage(msg);
                if (incomingEntity.Message.Type != EntityMessage.Null)
                    MessageBuffer.Enqueue(incomingEntity);
            }
            else
            {
                ProcessMsgBuffer();
                IncomingEntityMessage incomingEntity = ProcessNetMessage(msg);
                if (!_entities.ContainsKey(incomingEntity.Message.EntityId))
                {
                    MessageBuffer.Enqueue(incomingEntity);
                }
                else
                    _entities[incomingEntity.Message.EntityId].HandleNetworkMessage(incomingEntity);
            }
        }

        #endregion message processing

        private void CullDeletedEntities()
        {
            // Culling happens in updates.
            // It doesn't matter because to-be culled entities can't be accessed.
            // This should prevent most cases of "somebody is iterating while we're removing things"
            for (var i = 0; i < _allEntities.Count; i++)
            {
                var entity = _allEntities[i];
                if (!entity.Deleted)
                {
                    continue;
                }

                _allEntities.RemoveSwap(i);
                _entities.Remove(entity.Uid);

                // Process the one we just swapped next.
                i--;
            }
        }
    }
}
