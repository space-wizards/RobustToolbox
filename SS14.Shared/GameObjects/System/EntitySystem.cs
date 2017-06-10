using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects.System
{
    [IoCTarget(Disabled = true)]
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

        public virtual void RegisterMessageTypes()
        { }

        public virtual void SubscribeEvents()
        { }

        protected IEnumerable<IEntity> RelevantEntities => EntityManager.GetEntities(EntityQuery);

        public virtual void Initialize()
        {
        }

        public virtual void Shutdown()
        {
        }

        public virtual void HandleNetMessage(EntitySystemMessage sysMsg)
        {
            return;
        }

        public virtual void Update(float frameTime)
        { }
    }
}
