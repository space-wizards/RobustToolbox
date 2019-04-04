using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;

namespace SS14.Shared.GameObjects
{
    public abstract class EntityManager : IEntityManager
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

        [Dependency]
        private readonly IGameTiming _gameTiming;

        #endregion Dependencies

        public uint CurrentTick => _gameTiming.CurTick;

        public IComponentManager ComponentManager => _componentManager;
        public IEntityNetworkManager EntityNetManager => EntityNetworkManager;

        protected readonly Dictionary<EntityUid, IEntity> Entities = new Dictionary<EntityUid, IEntity>();

        /// <summary>
        /// List of all entities, used for iteration.
        /// </summary>
        protected readonly List<Entity> _allEntities = new List<Entity>();

        protected readonly Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();

        protected int NextUid = (int)EntityUid.FirstUid;

        private readonly Dictionary<Type, List<Delegate>> _eventSubscriptions
            = new Dictionary<Type, List<Delegate>>();

        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>>();

        private readonly Queue<Tuple<object, EntityEventArgs>> _eventQueue
            = new Queue<Tuple<object, EntityEventArgs>>();

        public bool Started { get; protected set; }

        #region IEntityManager Members

        public virtual void Initialize()
        {
            _network.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, HandleEntityNetworkMessage);
        }

        public virtual void Startup()
        {
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            EntitySystemManager.Shutdown();
            Started = false;
            _componentManager.Clear();
        }

        public virtual void Update(float frameTime)
        {
            ProcessMsgBuffer();
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

        public abstract IEntity SpawnEntity(string protoName);
        public abstract IEntity ForceSpawnEntityAt(string entityType, GridCoordinates coordinates);
        public abstract IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap);
        public abstract bool TrySpawnEntityAt(string entityType, Vector2 position, MapId argMap, out IEntity entity);
        public abstract bool TrySpawnEntityAt(string entityType, GridCoordinates coordinates, out IEntity entity);

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
                var transform = entity.Transform;
                if (FloatMath.CloseTo(transform.GridPosition.X, position.X) && FloatMath.CloseTo(transform.GridPosition.Y, position.Y))
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<IEntity> GetEntities()
        {
            // Manual index loop to allow adding to the list while iterating.
            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < _allEntities.Count; i++)
            {
                var entity = _allEntities[i];
                if (entity.Deleted)
                {
                    continue;
                }

                yield return entity;
            }
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public virtual void DeleteEntity(IEntity e)
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
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected Entity AllocEntity(string prototypeName, EntityUid? uid = null)
        {
            if (uid == null)
            {
                uid = new EntityUid(NextUid++);
            }

            if (EntityExists(uid.Value))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            var prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            var entity = prototype.AllocEntity(uid.Value, this);
            Entities[entity.Uid] = entity;
            _allEntities.Add(entity);
            return entity;
        }

        /// <summary>
        ///     Allocates and entity and loads components but does not do initialization.
        /// </summary>
        private protected Entity CreateEntity(string prototypeName, EntityUid? uid = null)
        {
            var entity = AllocEntity(prototypeName, uid);
            entity.Prototype.LoadEntity(entity, ComponentFactory, null);
            return entity;
        }

        private protected void LoadEntity(Entity entity, IEntityLoadContext context)
        {
            entity.Prototype.LoadEntity(entity, ComponentFactory, context);
        }

        private protected static void InitializeAndStartEntity(Entity entity)
        {
            InitializeEntity(entity);
            StartEntity(entity);
        }

        private protected static void InitializeEntity(Entity entity)
        {
            entity.InitializeComponents();
            entity.Initialize();
        }

        private protected static void StartEntity(Entity entity)
        {
            entity.StartAllComponents();
        }

        #endregion Entity Management

        #region ComponentEvents

        public void SubscribeEvent<T>(Delegate eventHandler, IEntityEventSubscriber s)
            where T : EntityEventArgs
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

        public void UnsubscribeEvent<T>(IEntityEventSubscriber s)
            where T : EntityEventArgs
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
            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
            {
                return;
            }

            foreach (var keyValuePair in val.ToList())
            {
                UnsubscribeEvent(keyValuePair.Key, keyValuePair.Value, subscriber);
            }
        }

        #endregion ComponentEvents

        #endregion IEntityManager Members

        #region ComponentEvents

        private void ProcessEventQueue()
        {
            while (_eventQueue.Count != 0)
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
            {
                return;
            }

            if (MessageBuffer.Count == 0)
            {
                return;
            }

            var misses = new List<IncomingEntityMessage>();

            while (MessageBuffer.Count != 0)
            {
                var incomingEntity = MessageBuffer.Dequeue();
                if (!Entities.TryGetValue(incomingEntity.Message.EntityUid, out var entity))
                {
                    incomingEntity.LastProcessingAttempt = DateTime.Now;
                    if ((incomingEntity.LastProcessingAttempt - incomingEntity.ReceivedTime).TotalSeconds > incomingEntity.Expires)
                        misses.Add(incomingEntity);
                }
                else
                {
                    entity.HandleNetworkMessage(incomingEntity);
                }
            }

            foreach (var miss in misses)
                MessageBuffer.Enqueue(miss);
        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg">Incoming raw network message</param>
        private void HandleEntityNetworkMessage(MsgEntity msg)
        {
            var incomingEntity = EntityNetworkManager.HandleEntityNetworkMessage(msg);
            // bad message or handled by something else
            if (incomingEntity == null)
                return;

            if (!Started)
            {
                if (incomingEntity.Message.Type != EntityMessageType.Error)
                {
                    MessageBuffer.Enqueue(incomingEntity);
                }
                return;
            }

            if (!Entities.TryGetValue(incomingEntity.Message.EntityUid, out var entity))
            {
                MessageBuffer.Enqueue(incomingEntity);
            }
            else
            {
                entity.HandleNetworkMessage(incomingEntity);
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
