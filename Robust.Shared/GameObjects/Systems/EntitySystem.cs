using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Shared.GameObjects.Systems
{
    /// <summary>
    ///     A subsystem that acts on all components of a type at once.
    /// </summary>
    /// <remarks>
    ///     This class is instantiated by the <c>EntitySystemManager</c>, and any IoC Dependencies will be resolved.
    /// </remarks>
    [Reflect(false)]
    public abstract class EntitySystem : IEntityEventSubscriber, IEntitySystem
    {
        [Dependency] protected readonly IEntityManager EntityManager;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager;
        [Dependency] protected readonly IEntityNetworkManager EntityNetworkManager;

        protected IEntityQuery EntityQuery;
        protected IEnumerable<IEntity> RelevantEntities => EntityManager.GetEntities(EntityQuery);

        private readonly Dictionary<Type, (CancellationTokenRegistration, TaskCompletionSource<EntitySystemMessage>)>
            _awaitingMessages
                = new Dictionary<Type, (CancellationTokenRegistration, TaskCompletionSource<EntitySystemMessage>)>();

        protected EntitySystem()
        {
            //EntityManager = IoCManager.Resolve<IEntityManager>();
            //EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            //EntityNetworkManager = IoCManager.Resolve<IEntityNetworkManager>();
        }

        public virtual void RegisterMessageTypes()
        {
        }

        public virtual void SubscribeEvents()
        {
        }

        /// <inheritdoc />
        public virtual void Initialize() { }

        /// <inheritdoc />
        public virtual void Update(float frameTime) { }

        /// <inheritdoc />
        public virtual void FrameUpdate(float frameTime) { }

        /// <inheritdoc />
        public virtual void Shutdown() { }

        /// <inheritdoc />
        public virtual void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            var type = message.GetType();
            if (_awaitingMessages.TryGetValue(type, out var awaiting))
            {
                var (_, tcs) = awaiting;
                tcs.TrySetResult(message);
                _awaitingMessages.Remove(type);
            }
        }

        public void RegisterMessageType<T>()
            where T : EntitySystemMessage
        {
            EntitySystemManager.RegisterMessageType<T>(this);
        }

        protected void SubscribeEvent<T>(EntityEventHandler<EntitySystemMessage> evh)
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.SubscribeEvent(evh, this);
        }

        protected void SubscribeEvent<T>(EntityEventHandler<T> evh)
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.SubscribeEvent(evh, this);
        }

        protected void UnsubscribeEvent<T>()
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(this);
        }

        protected void RaiseEvent(EntitySystemMessage message)
        {
            EntityManager.EventBus.RaiseEvent(this, message);
        }

        protected void QueueEvent(EntitySystemMessage message)
        {
            EntityManager.EventBus.QueueEvent(this, message);
        }

        protected void RaiseNetworkEvent(EntitySystemMessage message)
        {
            EntityNetworkManager.SendSystemNetworkMessage(message);
        }

        protected void RaiseNetworkEvent(EntitySystemMessage message, INetChannel channel)
        {
            EntityNetworkManager.SendSystemNetworkMessage(message, channel);
        }

        protected Task<T> AwaitNetMessage<T>(CancellationToken cancellationToken = default)
            where T : EntitySystemMessage
        {
            var type = typeof(T);
            if (_awaitingMessages.ContainsKey(type))
            {
                throw new InvalidOperationException("Cannot await the same message type twice at once.");
            }

            var tcs = new TaskCompletionSource<EntitySystemMessage>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingMessages.Remove(type);
                    tcs.TrySetCanceled();
                });
            }

            // Tiny trick so we can return T while the tcs is passed an EntitySystemMessage.
            async Task<T> DoCast(Task<EntitySystemMessage> task)
            {
                return (T) await task;
            }

            _awaitingMessages.Add(type, (reg, tcs));
            return DoCast(tcs.Task);
        }
    }
}
