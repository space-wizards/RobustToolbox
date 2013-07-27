using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;

namespace GameObject.System
{
    public abstract class EntitySystem
    {
        protected EntityManager EntityManager;

        private bool _initialized = false;
        private bool _shutdown = false;
        protected EntityQuery EntityQuery;

        protected List<Entity> RelevantEntities
        {
            get { return EntityManager.GetEntities(EntityQuery); }
        }
        
        public EntitySystem(EntityManager em)
        {
            EntityManager = em;
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
