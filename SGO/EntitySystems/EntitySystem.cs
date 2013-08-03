using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GO = GameObject;
using ServerInterfaces.GOC;

namespace SGO.EntitySystems
{
    public abstract class EntitySystem
    {
        protected EntityManager EntityManager;

        private bool _initialized = false;
        private bool _shutdown = false;
        protected GO.EntityQuery EntityQuery;

        protected List<GO.Entity> RelevantEntities
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
