using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;

namespace SS14.Shared.GameObjects.System
{
    /// <summary>
    /// A subsystem that acts on all components of a type at once.
    /// </summary>
    [Reflect(false)]
    public abstract class EntitySystem : IEntityEventSubscriber, IEntitySystem
    {
        protected readonly IEntityManager EntityManager;
        protected readonly IEntitySystemManager EntitySystemManager;
        protected IEntityQuery EntityQuery;

        public EntitySystem()
        {
            EntityManager = IoCManager.Resolve<IEntityManager>();
            EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        }

        protected IEnumerable<IEntity> RelevantEntities => EntityManager.GetEntities(EntityQuery);

        public virtual void RegisterMessageTypes()
        {
        }

        public virtual void SubscribeEvents()
        {
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {
        }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
        }

        /// <inheritdoc />
        public virtual void HandleNetMessage(EntitySystemMessage sysMsg)
        {
        }

        /// <inheritdoc />
        public virtual void Update(float frameTime)
        {
        }

        public void SubscribeEvent<T>(EntityEventHandler<EntityEventArgs> evh, IEntityEventSubscriber s) where T : EntityEventArgs
        {
            EntityManager.SubscribeEvent<T>(evh, s);
        }

        public void UnsubscribeEvent<T>(IEntityEventSubscriber s) where T : EntityEventArgs
        {
            EntityManager.UnsubscribeEvent<T>(s);
        }

        public void RaiseEvent(EntityEventArgs toRaise)
        {
            EntityManager.RaiseEvent(this, toRaise);
        }
    }
}
