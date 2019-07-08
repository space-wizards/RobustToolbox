using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public abstract class EntityManager : IEntityManager
    {
        #region Dependencies

        [Dependency]
#pragma warning disable 649
        private readonly IEntityNetworkManager EntityNetworkManager;

        [Dependency]
        private readonly IPrototypeManager PrototypeManager;

        [Dependency]
        protected readonly IEntitySystemManager EntitySystemManager;

        [Dependency]
        private readonly IComponentFactory ComponentFactory;

        [Dependency]
        private readonly INetManager _network;

        [Dependency]
        private readonly IComponentManager _componentManager;

        [Dependency]
        private readonly IGameTiming _gameTiming;

        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        #endregion Dependencies

        /// <inheritdoc />
        public GameTick CurrentTick => _gameTiming.CurTick;

        /// <inheritdoc />
        public IComponentManager ComponentManager => _componentManager;

        /// <inheritdoc />
        public IEntityNetworkManager EntityNetManager => EntityNetworkManager;

        /// <summary>
        ///     All entities currently stored in the manager.
        /// </summary>
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

        private readonly Queue<(object sender, EntityEventArgs eventArgs)> _eventQueue
            = new Queue<(object, EntityEventArgs)>();

        public bool Started { get; protected set; }

        #region IEntityManager Members

        public virtual void Initialize()
        {
            _network.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, HandleEntityNetworkMessage);

            _componentManager.ComponentRemoved += (sender, args) => RemoveSubscribedEvents(args.Component);
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
        public abstract IEntity SpawnEntityNoMapInit(string protoName);

        public abstract IEntity SpawnEntityAt(string entityType, GridCoordinates coordinates);

        public IEntity SpawnEntityAt(string entityType, MapCoordinates coordinates)
        {
            var map = _mapManager.GetMap(coordinates.MapId);
            var grid = map.FindGridAt(coordinates.Position);
            var gridCoords = new GridCoordinates(grid.WorldToLocal(coordinates.Position), grid);

            return SpawnEntityAt(entityType, gridCoords);
        }

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
            return query.Match(this);
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
            var entity = AllocEntity(uid);

            if (string.IsNullOrWhiteSpace(prototypeName))
                return entity;

            var prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            entity.Prototype = prototype;

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected Entity AllocEntity(EntityUid? uid = null)
        {
            if (uid == null)
            {
                uid = new EntityUid(NextUid++);
            }

            if (EntityExists(uid.Value))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            var entity = new Entity();

            entity.SetManagers(this);
            entity.SetUid(uid.Value);

            // allocate the required MetaDataComponent
            _componentManager.AddComponent<MetaDataComponent>(entity);

            Entities[entity.Uid] = entity;
            _allEntities.Add(entity);

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        private protected Entity CreateEntity(string prototypeName, EntityUid? uid = null)
        {
            if (prototypeName == null)
                return AllocEntity(uid);

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

        public void SubscribeEvent<T>(EntityEventHandler<T> eventHandler, IEntityEventSubscriber s)
            where T : EntityEventArgs
        {
            var eventType = typeof(T);
            if (!_eventSubscriptions.TryGetValue(eventType, out var subscriptions))
            {
                _eventSubscriptions.Add(eventType, new List<Delegate> { eventHandler });
            }
            else if (!subscriptions.Contains(eventHandler))
            {
                subscriptions.Add(eventHandler);
            }

            if (!_inverseEventSubscriptions.TryGetValue(s, out var inverseSubscription))
            {
                inverseSubscription = new Dictionary<Type, Delegate>
                {
                    {eventType, eventHandler}
                };

                _inverseEventSubscriptions.Add(
                    s,
                    inverseSubscription
                );
            }

            else if (!inverseSubscription.ContainsKey(eventType))
            {
                inverseSubscription.Add(eventType, eventHandler);
            }
        }

        public void UnsubscribeEvent<T>(IEntityEventSubscriber s)
            where T : EntityEventArgs
        {
            var eventType = typeof(T);

            if (_inverseEventSubscriptions.TryGetValue(s, out var inverse)
                && inverse.TryGetValue(eventType, out var @delegate))
            {
                UnsubscribeEvent(eventType, @delegate, s);
            }
        }

        private void UnsubscribeEvent(Type eventType, Delegate evh, IEntityEventSubscriber s)
        {
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions) && subscriptions.Contains(evh))
            {
                subscriptions.Remove(evh);
            }

            if (_inverseEventSubscriptions.TryGetValue(s, out var inverse) && inverse.ContainsKey(eventType))
            {
                inverse.Remove(eventType);
            }
        }

        public void RaiseEvent(object sender, EntityEventArgs toRaise)
        {
            ProcessSingleEvent((sender, toRaise));
        }

        public void QueueEvent(object sender, EntityEventArgs toRaise)
        {
            _eventQueue.Enqueue((sender, toRaise));
        }

        public void RemoveSubscribedEvents(IEntityEventSubscriber subscriber)
        {
            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
            {
                return;
            }

            foreach (var (type, @delegate) in val.ToList())
            {
                UnsubscribeEvent(type, @delegate, subscriber);
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

        private void ProcessSingleEvent((object sender, EntityEventArgs eventArgs) argsTuple)
        {
            var (sender, eventArgs) = argsTuple;
            var eventType = eventArgs.GetType();
            if (!_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                return;
            }

            foreach (var handler in subs)
            {
                handler.DynamicInvoke(sender, eventArgs);
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
