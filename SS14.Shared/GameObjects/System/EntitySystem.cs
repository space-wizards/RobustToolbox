using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;

namespace SS14.Shared.GameObjects.System
{
    /// <summary>
    ///     A subsystem that acts on all components of a type at once.
    /// </summary>
    [Reflect(false)]
    public abstract class EntitySystem : IEntityEventSubscriber, IEntitySystem
    {
        protected readonly IEntityManager EntityManager;
        protected readonly IEntitySystemManager EntitySystemManager;
        protected readonly IEntityNetworkManager EntityNetworkManager;
        protected IEntityQuery EntityQuery;

        protected IEnumerable<IEntity> RelevantEntities => EntityManager.GetEntities(EntityQuery);

        public EntitySystem()
        {
            EntityManager = IoCManager.Resolve<IEntityManager>();
            EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            EntityNetworkManager = IoCManager.Resolve<IEntityNetworkManager>();
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
        public virtual void HandleNetMessage(INetChannel channel, EntitySystemMessage message) { }

        public void RegisterMessageType<T>()
            where T : EntitySystemMessage
        {
            EntitySystemManager.RegisterMessageType<T>(this);
        }

        protected void SubscribeEvent<T>(EntityEventHandler<EntitySystemMessage> evh)
            where T : EntitySystemMessage
        {
            EntityManager.SubscribeEvent<T>(evh, this);
        }

        protected void UnsubscribeEvent<T>()
            where T : EntitySystemMessage
        {
            EntityManager.UnsubscribeEvent<T>(this);
        }

        protected void RaiseEvent(EntitySystemMessage message)
        {
            EntityManager.RaiseEvent(this, message);
        }

        protected void RaiseNetworkEvent(EntitySystemMessage message)
        {
            EntityNetworkManager.SendSystemNetworkMessage(message);
        }
    }
}
