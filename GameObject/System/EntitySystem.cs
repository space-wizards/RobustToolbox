using System.Collections.Generic;
using SS13_Shared.GO;

namespace GameObject.System
{
    public abstract class EntitySystem
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

        public abstract void Update(float frameTime);
    }
}