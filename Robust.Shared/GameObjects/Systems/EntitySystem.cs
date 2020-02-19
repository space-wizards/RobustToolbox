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

        protected void SubscribeEvent<T>(EntityEventHandler<T> handler)
            where T : EntitySystemMessage
        {
            EntityManager.EventBus.SubscribeEvent(handler, this);
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
            return EntityManager.EventBus.AwaitEvent<T>(cancellationToken);
        }

        #endregion
    }
}
