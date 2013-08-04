using System.Collections.Generic;

namespace GameObject.System
{
    public abstract class EntitySystem
    {
        protected EntityManager EntityManager;
        protected EntityQuery EntityQuery;

        private bool _initialized;
        private bool _shutdown;

        public EntitySystem(EntityManager em)
        {
            EntityManager = em;
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

        public abstract void Update(float frameTime);
    }
}