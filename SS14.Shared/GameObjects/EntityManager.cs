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
using SS14.Shared.Interfaces.Network;
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
        private readonly INetManager _network;
        [Dependency]
        private readonly IComponentManager _componentManager;
        # endregion Dependencies

        protected readonly Dictionary<EntityUid, IEntity> Entities = new Dictionary<EntityUid, IEntity>();
        /// <summary>
        /// List of all entities, used for iteration.
        /// </summary>
        protected readonly List<Entity> _allEntities = new List<Entity>();
        protected readonly Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();

        // This MUST start > 0
        protected int NextUid = 1;

        private readonly Dictionary<Type, List<Delegate>> _eventSubscriptions
            = new Dictionary<Type, List<Delegate>>();
        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>>();

        private readonly Queue<Tuple<object, EntityEventArgs>> _eventQueue
            = new Queue<Tuple<object, EntityEventArgs>>();

        public bool Started { get; protected set; }
        public bool MapsInitialized { get; set; } = false;

        #region IEntityManager Members

        public virtual void Initialize()
        {
            _network.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, message => HandleEntityNetworkMessage((MsgEntity)message));
        }

        public virtual void Startup()
        {
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            EntitySystemManager.Shutdown();
            Started = false;
            _componentManager.Cull();
        }

        public virtual void Update(float frameTime)
        {
            EntitySystemManager.Update(frameTime);
            ProcessEventQueue();
            CullDeletedEntities();
        }

        public virtual void FrameUpdate(float frameTime)
        {
            EntitySystemManager.FrameUpdate(frameTime);
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
        /// <param name="uid"></param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public IEntity GetEntity(EntityUid uid)
        {
            return Entities[uid];
        }

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        public bool TryGetEntity(EntityUid uid, out IEntity entity)
        {
            if (Entities.TryGetValue(uid, out entity) && !entity.Deleted)
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

        public void DeleteEntity(EntityUid uid)
        {
            if (TryGetEntity(uid, out var entity))
            {
                DeleteEntity(entity);
            }
        }

        public bool EntityExists(EntityUid uid)
        {
            return TryGetEntity(uid, out var _);
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
            CullDeletedEntities();
        }

        /// <summary>
        /// Creates an entity and adds it to the entity dictionary
        /// </summary>
        /// <param name="prototypeName">name of entity template to execute</param>
        /// <param name="uid">UID to give to the new entity.</param>
        /// <returns>spawned entity</returns>
        public Entity SpawnEntity(string prototypeName, EntityUid? uid = null)
        {
            if (uid == null)
            {
                uid = new EntityUid(NextUid++);
            }

            if (EntityExists(uid.Value))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            EntityPrototype prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            Entity entity = prototype.CreateEntity(uid.Value, this, EntityNetworkManager, ComponentFactory);
            Entities[uid.Value] = entity;
            _allEntities.Add(entity);

            return entity;
        }

        protected static void InitializeEntity(Entity entity)
        {
            entity.InitializeComponents();
            entity.Initialize();

            entity.StartAllComponents();
        }

        /// <summary>
        /// Initializes all entities that haven't been initialized yet.
        /// </summary>
        protected void InitializeEntities()
        {
            for (var i = 0; i < _allEntities.Count; i++)
            {
                var ent = _allEntities[i];

                if (ent.Deleted || ent.Initialized)
                    continue;

                InitializeEntity(ent);
            }
        }

        public void GetEntityData()
        {
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
            if (!Started)
                return;
            if (!MessageBuffer.Any()) return;
            var misses = new List<IncomingEntityMessage>();

            while (MessageBuffer.Any())
            {
                IncomingEntityMessage incomingEntity = MessageBuffer.Dequeue();
                if (!Entities.ContainsKey(incomingEntity.Message.EntityUid))
                {
                    incomingEntity.LastProcessingAttempt = DateTime.Now;
                    if ((incomingEntity.LastProcessingAttempt - incomingEntity.ReceivedTime).TotalSeconds > incomingEntity.Expires)
                        misses.Add(incomingEntity);
                }
                else
                    Entities[incomingEntity.Message.EntityUid].HandleNetworkMessage(incomingEntity);
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
            if (!Started)
            {
                var incomingEntity = ProcessNetMessage(msg);
                if (incomingEntity.Message.Type != EntityMessageType.Error)
                    MessageBuffer.Enqueue(incomingEntity);
            }
            else
            {
                ProcessMsgBuffer();
                var incomingEntity = ProcessNetMessage(msg);

                // bad message or handled by something else
                if (incomingEntity == null)
                    return;

                if (!Entities.ContainsKey(incomingEntity.Message.EntityUid))
                {
                    MessageBuffer.Enqueue(incomingEntity);
                }
                else
                    Entities[incomingEntity.Message.EntityUid].HandleNetworkMessage(incomingEntity);
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
                Entities.Remove(entity.Uid);

                // Process the one we just swapped next.
                i--;
            }
        }
    }

    public enum EntityMessageType
    {
        Error = 0,
        ComponentMessage,
        EntityMessage,
        SystemMessage
    }
}
