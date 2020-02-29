using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
    [Reflect(false), PublicAPI]
    public abstract class EntitySystem : IEntitySystem
    {
        [Dependency] protected readonly IEntityManager EntityManager;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager;
        [Dependency] protected readonly IEntityNetworkManager EntityNetworkManager;

        protected IEntityQuery EntityQuery;
        protected IEnumerable<IEntity> RelevantEntities => EntityManager.GetEntities(EntityQuery);

        /// <inheritdoc />
        public virtual void Initialize() { }

        /// <inheritdoc />
        public virtual void Update(float frameTime) { }

        /// <inheritdoc />
        public virtual void FrameUpdate(float frameTime) { }

        /// <inheritdoc />
        public virtual void Shutdown() { }

        #region Event Proxy

        protected void SubscribeNetworkEvent<T>(EntityEventHandler<T> handler)
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.SubscribeEvent(EventSource.Network, this, handler);
        }

        protected void SubscribeLocalEvent<T>(EntityEventHandler<T> handler)
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.SubscribeEvent(EventSource.Local, this, handler);
        }

        protected void UnsubscribeNetworkEvent<T>()
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Network, this);
        }

        protected void UnsubscribeLocalEvent<T>()
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Local, this);
        }

        protected void RaiseLocalEvent(EntitySystemMessage message)
        {
            EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
        }

        protected void QueueLocalEvent(EntitySystemMessage message)
        {
            EntityManager.EventBus.QueueEvent(EventSource.Local, message);
        }

        protected void RaiseNetworkEvent(EntitySystemMessage message)
        {
            EntityNetworkManager.SendSystemNetworkMessage(message);
        }

        protected void RaiseNetworkEvent(EntitySystemMessage message, INetChannel channel)
        {
            EntityNetworkManager.SendSystemNetworkMessage(message, channel);
        }

        protected Task<T> AwaitNetworkEvent<T>(CancellationToken cancellationToken)
            where T : EntitySystemMessage
        {
            return EntityManager.EventBus.AwaitEvent<T>(EventSource.Network, cancellationToken);
        }

        #endregion
    }
}
