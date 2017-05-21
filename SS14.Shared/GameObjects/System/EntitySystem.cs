using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects.System
{
    public abstract class EntitySystem: IEntityEventSubscriber
    {
        protected EntityManager EntityManager;
        protected EntitySystemManager EntitySystemManager;
        protected EntityQuery EntityQuery;

        private bool _initialized;
        private bool _shutdown;

        public EntitySystem(EntityManager em, EntitySystemManager esm)
        {
            EntityManager = em;
            EntitySystemManager = esm;
        }

        public EntitySystem()
        {}

        public virtual void RegisterMessageTypes()
        {}

        public virtual void SubscribeEvents()
        {}

        protected List<Entity> RelevantEntities
        {
            get { return EntityManager.GetEntities(EntityQuery); }
        }

        public virtual void Initialize()
        {
            _initialized = true;
        }

        public virtual void Shutdown()
        {
            _shutdown = true;
        }

        public virtual void HandleNetMessage(EntitySystemMessage sysMsg)
        {
            return;
        }

        public virtual void Update(float frameTime)
        {}
    }
}
