using System.Collections.Generic;
using GO = GameObject;

namespace SGO.EntitySystems
{
    public abstract class EntitySystem
    {
        protected EntityManager EntityManager;
        protected GO.EntityQuery EntityQuery;

        private bool _initialized;
        private bool _shutdown;

        public EntitySystem(EntityManager em)
        {
            EntityManager = em;
        }

        protected List<GO.Entity> RelevantEntities
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